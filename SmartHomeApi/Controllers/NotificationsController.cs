using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// התראות הבית.
/// ההתראות מחושבות מחדש בכל קריאה מתוך הנתונים הקיימים — אין טבלת התראות,
/// אין מצב "נקראה" ואין שליחה יזומה. כל עוד אין כאן שירות רקע, התראה
/// מתגלה רק כשנכנסים לאפליקציה; שליחה אמיתית היא הרחבה עתידית.
///
/// כל הישויות שנקראות כאן מממשות IHouseholdOwned, ולכן ה-Global Query Filter
/// כבר מגביל כל שאילתה למשק הבית של המשתמש המחובר — בדיוק כמו בקונטרולרים המקבילים.
///
/// שני פערים ידועים מול מפרט הפרויקט, מאושרים מראש, ושניהם הרחבה עתידית:
///
/// 1. אין התראת תקציב. המפרט מבקש התראה בהגעה ל-80% מהתקרה, אבל ל-BudgetEntry
///    יש רק Amount, Type, Category ו-Date — אין שום שדה תקרה, ובלי תקרה אין
///    ממה לגזור אחוז ניצול. התראה כזו דורשת קודם מודל תקרות חודשיות לקטגוריה,
///    ורק אחריו אפשר יהיה להוסיף כאן BuildBudgetNotificationsAsync.
///
/// 2. אין התראת "תרופות כל יום בשעה קבועה". היא דורשת תזמון יומי בשעה שנקבעה,
///    ולא ניתנת למימוש במודל שמחושב בכניסה לאפליקציה. מהמודול הרפואי נכללים
///    כאן תורים קרובים בלבד.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    // ספים בימים, לפי מפרט הפרויקט.
    private const int EventSoonDays = 7;
    private const int EventUrgentDays = 1;
    private const int GiftSoonDays = 7;
    private const int MedicalSoonDays = 7;
    private const int MedicalUrgentDays = 1;
    private const int VehicleSoonDays = 30;
    private const int VehicleUrgentDays = 7;

    private const string SeverityInfo = "info";
    private const string SeverityWarning = "warning";
    private const string SeverityUrgent = "urgent";

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// כל ההתראות הפעילות של הבית, ממוינות לפי התאריך שההתראה מדברת עליו —
    /// הקרובות ביותר קודם.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        // בלי משק בית אין על מה להתריע. הפילטר הגלובלי ממילא לא היה מחזיר כלום.
        if (_currentUser.HouseholdId is null)
            return Ok(Array.Empty<NotificationDto>());

        var now = DateTime.UtcNow;

        var notifications = new List<NotificationDto>();
        notifications.AddRange(await BuildEventNotificationsAsync(now));
        notifications.AddRange(await BuildGiftNotificationsAsync(now));
        notifications.AddRange(await BuildInventoryNotificationsAsync(now));
        notifications.AddRange(await BuildMedicalNotificationsAsync(now));
        notifications.AddRange(await BuildVehicleNotificationsAsync(now));

        return Ok(notifications.OrderBy(n => n.Date).ToList());
    }

    /// <summary>אירוע ביומן בשבוע הקרוב.</summary>
    private async Task<IEnumerable<NotificationDto>> BuildEventNotificationsAsync(DateTime now)
    {
        var until = now.AddDays(EventSoonDays);

        var events = await _db.Events
            .AsNoTracking()
            .Where(e => e.StartDate >= now && e.StartDate <= until)
            .ToListAsync();

        return events.Select(e => new NotificationDto(
            "event",
            SeverityFor(e.StartDate, now, EventUrgentDays),
            e.Title,
            $"האירוע מתקיים {RelativeDay(DaysUntil(e.StartDate, now))}",
            e.Id.ToString(),
            e.StartDate));
    }

    /// <summary>
    /// מתנה שטרם נקנתה לאירוע שמתקרב.
    /// כשהמתנה מקושרת לאירוע ביומן התאריך נלקח מהאירוע, אחרת מתאריך המתנה עצמה.
    /// ההודעה מציינת רק את קרבת המועד — אין ולא יהיה כאן שום אזכור של סכום.
    /// </summary>
    private async Task<IEnumerable<NotificationDto>> BuildGiftNotificationsAsync(DateTime now)
    {
        var until = now.AddDays(GiftSoonDays);

        var gifts = await _db.Gifts
            .AsNoTracking()
            .Include(g => g.Event)
            .Where(g => !g.IsPurchased)
            .ToListAsync();

        return gifts
            .Select(g => new { Gift = g, Date = g.Event != null ? g.Event.StartDate : g.Date })
            .Where(x => x.Date >= now && x.Date <= until)
            .Select(x => new NotificationDto(
                "gift",
                SeverityFor(x.Date, now, EventUrgentDays),
                $"מתנה ל{x.Gift.RecipientName}",
                $"האירוע {RelativeDay(DaysUntil(x.Date, now))} והמתנה עדיין לא נקנתה",
                x.Gift.Id.ToString(),
                x.Date));
    }

    /// <summary>
    /// פריט מלאי שירד לסף שנקבע לו.
    /// פריט בלי סף (MinQuantity אפס) לא מתריע — לא הוגדרה לו כמות מינימלית.
    /// אין כאן תאריך עתידי, ולכן התאריך הוא הרגע הנוכחי: זהו מצב קיים
    /// ולא אירוע שעתיד לקרות, והמיון מציב אותו לפני ההתראות העתידיות.
    /// </summary>
    private async Task<IEnumerable<NotificationDto>> BuildInventoryNotificationsAsync(DateTime now)
    {
        var items = await _db.InventoryItems
            .AsNoTracking()
            .Where(i => i.MinQuantity > 0 && i.Quantity <= i.MinQuantity)
            .ToListAsync();

        // תאריכי הישויות חוזרים מבסיס הנתונים כ-Unspecified ומסודרים בלי סיומת אזור זמן,
        // ואילו UtcNow היה מסודר עם Z. ה-client מפרש את שתי הצורות אחרת, וההתראות
        // היו נערמות בסדר שגוי ביחס למיון שנקבע כאן. אותה צורה לכולן.
        var stamp = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);

        return items.Select(i => new NotificationDto(
            "inventory",
            i.Quantity == 0 ? SeverityUrgent : SeverityWarning,
            i.Name,
            i.Quantity == 0
                ? "המלאי אזל"
                : $"נותרו {i.Quantity}{UnitSuffix(i.Unit)} — הסף הוא {i.MinQuantity}",
            i.Id.ToString(),
            stamp));
    }

    /// <summary>תור רפואי קרוב. תרופות יומיות אינן נכללות — ראה הערת המחלקה.</summary>
    private async Task<IEnumerable<NotificationDto>> BuildMedicalNotificationsAsync(DateTime now)
    {
        var until = now.AddDays(MedicalSoonDays);

        var records = await _db.MedicalRecords
            .AsNoTracking()
            .Include(r => r.Member)
            .Where(r => r.NextAppointment != null
                        && r.NextAppointment >= now
                        && r.NextAppointment <= until)
            .ToListAsync();

        return records.Select(r =>
        {
            var date = r.NextAppointment!.Value;
            var who = r.Member is null ? string.Empty : $" של {r.Member.FullName}";

            return new NotificationDto(
                "medical",
                SeverityFor(date, now, MedicalUrgentDays),
                r.Title,
                $"התור{who} {RelativeDay(DaysUntil(date, now))}",
                r.Id.ToString(),
                date);
        });
    }

    /// <summary>
    /// טסט וביטוח שמתקרבים לפקיעה, כל אחד כהתראה נפרדת.
    /// תאריך שכבר עבר נכלל גם הוא, כרמת הדחיפות הגבוהה ביותר — רכב שהטסט שלו
    /// פג אתמול הוא בדיוק המקרה שאסור שייעלם מהרשימה.
    /// </summary>
    private async Task<IEnumerable<NotificationDto>> BuildVehicleNotificationsAsync(DateTime now)
    {
        var until = now.AddDays(VehicleSoonDays);

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(v => (v.TestExpiry != null && v.TestExpiry <= until)
                        || (v.InsuranceExpiry != null && v.InsuranceExpiry <= until))
            .ToListAsync();

        var notifications = new List<NotificationDto>();

        foreach (var vehicle in vehicles)
        {
            notifications.AddRange(ExpiryNotifications(vehicle.Id, vehicle.Name, vehicle.TestExpiry, "הטסט", until, now));
            notifications.AddRange(ExpiryNotifications(vehicle.Id, vehicle.Name, vehicle.InsuranceExpiry, "הביטוח", until, now));
        }

        return notifications;
    }

    /// <summary>
    /// התראה אחת לתאריך פקיעה בודד, או כלום כשאין תאריך או שהוא רחוק מדי.
    /// מוחזר כרצף כדי שהקורא יוכל פשוט לצרף אותו.
    /// </summary>
    private static IEnumerable<NotificationDto> ExpiryNotifications(
        int vehicleId, string vehicleName, DateTime? expiry, string label, DateTime until, DateTime now)
    {
        if (expiry is null || expiry > until)
            yield break;

        var date = expiry.Value;
        var days = DaysUntil(date, now);

        yield return new NotificationDto(
            "vehicle",
            days < 0 ? SeverityUrgent : SeverityFor(date, now, VehicleUrgentDays),
            vehicleName,
            $"{label} פג {RelativeDay(days)}",
            vehicleId.ToString(),
            date);
    }

    /// <summary>
    /// דחיפות לפי מרחק בימים: בתוך סף הדחיפות — urgent, ובתוך השבוע — warning.
    /// מעבר לכך זו התראה מקדימה בלבד.
    /// </summary>
    private static string SeverityFor(DateTime date, DateTime now, int urgentDays)
    {
        var days = DaysUntil(date, now);

        if (days <= urgentDays) return SeverityUrgent;
        if (days <= EventSoonDays) return SeverityWarning;
        return SeverityInfo;
    }

    /// <summary>
    /// הפרש בימי לוח ולא בשעות, כדי ש"מחר בבוקר" ייחשב יום אחד גם כשעכשיו ערב.
    /// ערך שלילי = התאריך כבר עבר.
    /// </summary>
    private static int DaysUntil(DateTime date, DateTime now) => (date.Date - now.Date).Days;

    /// <summary>ניסוח הקרבה בעברית, כולל תאריכים שכבר עברו.</summary>
    private static string RelativeDay(int days) => days switch
    {
        < -1 => $"לפני {-days} ימים",
        -1 => "אתמול",
        0 => "היום",
        1 => "מחר",
        _ => $"בעוד {days} ימים"
    };

    /// <summary>יחידת המידה נוספת רק כשהוגדרה, כדי לא לקבל "נותרו 2 " עם רווח תלוי.</summary>
    private static string UnitSuffix(string? unit) =>
        string.IsNullOrWhiteSpace(unit) ? string.Empty : $" {unit.Trim()}";
}
