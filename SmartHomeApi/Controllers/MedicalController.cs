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
/// תיק רפואי של בני הבית.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על MedicalRecord,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// הפילטור לפי בן משפחה נעשה כולו ב-client, ולכן אין כאן פרמטר סינון.
/// אין כאן שום מנגנון תזכורות או התראות — רק שמירה ושליפה של רשומות.
/// </summary>
[ApiController]
[Route("api/medical")]
[Authorize]
[Produces("application/json")]
public class MedicalController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;

    public MedicalController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    /// <summary>רשומות התיק הרפואי של הבית, מהאחרונה לישנה.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MedicalRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var records = await BaseQuery()
            .AsNoTracking()
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        return Ok(records.Select(MedicalMapping.ToDto));
    }

    /// <summary>הוספת רשומה חדשה. פתוח לכל בני הבית.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MedicalRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateMedicalRecordRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!MedicalMapping.TryParseType(request.Type, out var type))
            return BadRequest(new MessageResponse("סוג הרשומה אינו תקין"));

        var (memberId, memberError) = await ResolveMemberAsync(request.MemberId, householdId.Value);
        if (memberError is not null)
            return BadRequest(new MessageResponse(memberError));

        var (nextAppointment, dateError) = ParseNextAppointment(request.NextAppointment);
        if (dateError is not null)
            return BadRequest(new MessageResponse(dateError));

        var record = new MedicalRecord
        {
            MemberId = memberId!,
            Type = type,
            Title = request.Title.Trim(),
            Date = request.Date!.Value,
            Doctor = MedicalMapping.Normalize(request.Doctor),
            Clinic = MedicalMapping.Normalize(request.Clinic),
            Notes = MedicalMapping.Normalize(request.Notes),
            NextAppointment = nextAppointment,
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            AddedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.MedicalRecords.Add(record);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(m => m.Id == record.Id);
        var dto = MedicalMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "medical:created", dto);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>
    /// עדכון מלא של רשומה קיימת. פתוח לכל בני הבית.
    /// ה-client לא קורא לפעולה הזו כרגע — היא קיימת לשלמות ה-API.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MedicalRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateMedicalRecordRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var record = await BaseQuery().FirstOrDefaultAsync(m => m.Id == id);
        if (record is null)
            return NotFound(new MessageResponse("הרשומה לא נמצאה"));

        if (!MedicalMapping.TryParseType(request.Type, out var type))
            return BadRequest(new MessageResponse("סוג הרשומה אינו תקין"));

        var (memberId, memberError) = await ResolveMemberAsync(request.MemberId, householdId.Value);
        if (memberError is not null)
            return BadRequest(new MessageResponse(memberError));

        var (nextAppointment, dateError) = ParseNextAppointment(request.NextAppointment);
        if (dateError is not null)
            return BadRequest(new MessageResponse(dateError));

        record.MemberId = memberId!;
        record.Type = type;
        record.Title = request.Title.Trim();
        record.Date = request.Date!.Value;
        record.Doctor = MedicalMapping.Normalize(request.Doctor);
        record.Clinic = MedicalMapping.Normalize(request.Clinic);
        record.Notes = MedicalMapping.Normalize(request.Notes);
        record.NextAppointment = nextAppointment;

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(m => m.Id == record.Id);
        var dto = MedicalMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "medical:updated", dto);

        return Ok(dto);
    }

    /// <summary>
    /// מחיקת רשומה. מנהל בלבד — כפתור המחיקה ב-MedicalPage.tsx מוצג רק למנהל,
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

        var record = await _db.MedicalRecords.FirstOrDefaultAsync(m => m.Id == id);
        if (record is null)
            return NotFound(new MessageResponse("הרשומה לא נמצאה"));

        _db.MedicalRecords.Remove(record);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "medical:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("הרשומה נמחקה"));
    }

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<MedicalRecord> BaseQuery() => _db.MedicalRecords
        .Include(m => m.Member)
        .Include(m => m.AddedBy);

    /// <summary>
    /// מוודא שבן המשפחה שנבחר הוא חבר באותו משק בית.
    /// בלי הבדיקה הזו אפשר היה לשייך רשומה רפואית למשתמש מבית אחר ולדלוף את שמו.
    /// בניגוד לאחראי במשימות, כאן השדה חובה ולכן ערך ריק הוא שגיאה.
    /// </summary>
    private async Task<(string? Id, string? Error)> ResolveMemberAsync(string? memberId, int householdId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            return (null, "יש לבחור בן משפחה");

        var id = memberId.Trim();
        var exists = await _db.Users.AnyAsync(u => u.Id == id && u.HouseholdId == householdId);

        return exists ? (id, null) : (null, "המשתמש שנבחר אינו חבר בבית הזה");
    }

    /// <summary>ה-client שולח מחרוזת ריקה כשלא נבחר תאריך לתור הבא.</summary>
    private static (DateTime? Value, string? Error) ParseNextAppointment(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, null);

        var parsed = DateTime.TryParse(
            raw.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var value);

        return parsed ? (value, null) : (null, "תאריך התור הבא אינו תקין");
    }
}
