using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// ניהול רכבי הבית — טיפולים, ביטוח וטסט.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על Vehicle,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// התראות "טסט/ביטוח עומדים לפוג" מחושבות ב-client מתוך התאריכים
/// (daysUntil ב-VehiclePage.tsx) — אין כאן מנגנון התראות בצד השרת.
/// </summary>
[ApiController]
[Route("api/vehicles")]
[Authorize]
[Produces("application/json")]
public class VehiclesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;

    public VehiclesController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    /// <summary>
    /// רשימת רכבי הבית. הרכב שהטסט שלו הכי קרוב מופיע ראשון,
    /// רכבים בלי תאריך טסט בסוף, ובתוך כל קבוצה לפי שם.
    /// ה-client לא ממיין בעצמו (VehiclePage.tsx:120 עושה map ישיר),
    /// ולכן הסדר שנקבע כאן הוא הסדר שנראה על המסך.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await BaseQuery()
            .AsNoTracking()
            .OrderBy(v => v.TestExpiry == null)
            .ThenBy(v => v.TestExpiry)
            .ThenBy(v => v.Name)
            .ToListAsync();

        return Ok(vehicles.Select(VehicleMapping.ToDto));
    }

    /// <summary>הוספת רכב חדש. פתוח לכל בני הבית.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateVehicleRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var (dates, dateError) = ParseDates(request);
        if (dateError is not null)
            return BadRequest(new MessageResponse(dateError));

        var vehicle = new Vehicle
        {
            Name = request.Name.Trim(),
            LicensePlate = VehicleMapping.NormalizePlate(request.PlateNumber),
            Year = request.Year,
            LastService = dates.LastService,
            NextService = dates.NextService,
            InsuranceExpiry = dates.Insurance,
            TestExpiry = dates.Test,
            FuelType = VehicleMapping.Normalize(request.FuelType),
            Notes = VehicleMapping.Normalize(request.Notes),
            // Model לא נקבע כאן — אין לו שדה בטופס, והוא נשאר null.
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            AddedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(v => v.Id == vehicle.Id);
        var dto = VehicleMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "vehicle:created", dto);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>עדכון מלא של רכב קיים. פתוח לכל בני הבית.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateVehicleRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var vehicle = await BaseQuery().FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle is null)
            return NotFound(new MessageResponse("הרכב לא נמצא"));

        var (dates, dateError) = ParseDates(request);
        if (dateError is not null)
            return BadRequest(new MessageResponse(dateError));

        vehicle.Name = request.Name.Trim();
        vehicle.LicensePlate = VehicleMapping.NormalizePlate(request.PlateNumber);
        vehicle.Year = request.Year;
        vehicle.LastService = dates.LastService;
        vehicle.NextService = dates.NextService;
        vehicle.InsuranceExpiry = dates.Insurance;
        vehicle.TestExpiry = dates.Test;
        vehicle.FuelType = VehicleMapping.Normalize(request.FuelType);
        vehicle.Notes = VehicleMapping.Normalize(request.Notes);
        // Model לא נגזר מהבקשה ולכן נשאר כפי שהוא.

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(v => v.Id == vehicle.Id);
        var dto = VehicleMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "vehicle:updated", dto);

        return Ok(dto);
    }

    /// <summary>
    /// מחיקת רכב. מנהל בלבד — כפתור המחיקה ב-VehiclePage.tsx:128 מוצג רק למנהל,
    /// והכלל נאכף גם כאן ולא רק בממשק.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!_currentUser.IsManager)
            return Forbid();

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle is null)
            return NotFound(new MessageResponse("הרכב לא נמצא"));

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "vehicle:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("הרכב נמחק"));
    }

    /// <summary>ארבעת התאריכים של הרכב לאחר פענוח.</summary>
    private record VehicleDates(
        DateTime? LastService,
        DateTime? NextService,
        DateTime? Insurance,
        DateTime? Test);

    /// <summary>
    /// פענוח ארבעת התאריכים שמגיעים כמחרוזות.
    /// עוצר על השגיאה הראשונה ומחזיר הודעה בעברית עם שם השדה,
    /// כדי שהמשתמש יידע איזה תאריך לתקן.
    /// </summary>
    private static (VehicleDates Dates, string? Error) ParseDates(CreateVehicleRequest request)
    {
        var fields = new (string? Raw, string Label)[]
        {
            (request.LastService, "טיפול אחרון"),
            (request.NextService, "טיפול הבא"),
            (request.Insurance, "ביטוח"),
            (request.Test, "טסט")
        };

        var values = new DateTime?[fields.Length];

        for (var i = 0; i < fields.Length; i++)
        {
            var (value, error) = ParseDate(fields[i].Raw, fields[i].Label);
            if (error is not null)
                return (new VehicleDates(null, null, null, null), error);

            values[i] = value;
        }

        return (new VehicleDates(values[0], values[1], values[2], values[3]), null);
    }

    /// <summary>
    /// מחרוזת ריקה היא "לא נבחר תאריך" ולא שגיאה — כל ארבעת השדות אופציונליים.
    /// אותה תבנית בדיוק כמו ParseNextAppointment ב-MedicalController.
    /// </summary>
    private static (DateTime? Value, string? Error) ParseDate(string? raw, string label)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, null);

        var parsed = DateTime.TryParse(
            raw.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var value);

        return parsed ? (value, null) : (null, $"תאריך {label} אינו תקין");
    }

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<Vehicle> BaseQuery() => _db.Vehicles
        .Include(v => v.AddedBy);
}
