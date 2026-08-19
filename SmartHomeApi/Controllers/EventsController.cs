using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// לוח השנה והאירועים של הבית.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על Event,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// </summary>
[ApiController]
[Route("api/events")]
[Authorize]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;

    public EventsController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    /// <summary>רשימת האירועים של הבית, לפי סדר התאריכים.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await BaseQuery()
            .AsNoTracking()
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        return Ok(events.Select(EventMapping.ToDto));
    }

    /// <summary>יצירת אירוע חדש. פתוח לכל בני הבית.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var ev = new Event
        {
            Title = request.Title.Trim(),
            Description = EventMapping.Normalize(request.Description) ?? string.Empty,
            StartDate = request.StartDate!.Value,
            AllDay = request.AllDay,
            Color = EventMapping.Normalize(request.Color) ?? EventMapping.DefaultColor,
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            CreatedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(e => e.Id == ev.Id);
        var dto = EventMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "event:created", dto);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>עדכון מלא של אירוע קיים. פתוח לכל בני הבית.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateEventRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var ev = await BaseQuery().FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null)
            return NotFound(new MessageResponse("האירוע לא נמצא"));

        ev.Title = request.Title.Trim();
        ev.Description = EventMapping.Normalize(request.Description) ?? string.Empty;
        ev.StartDate = request.StartDate!.Value;
        ev.AllDay = request.AllDay;
        ev.Color = EventMapping.Normalize(request.Color) ?? EventMapping.DefaultColor;

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(e => e.Id == ev.Id);
        var dto = EventMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "event:updated", dto);

        return Ok(dto);
    }

    /// <summary>
    /// מחיקת אירוע. מנהל בלבד — כפתור המחיקה ב-CalendarPage.tsx מוצג רק למנהל,
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

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null)
            return NotFound(new MessageResponse("האירוע לא נמצא"));

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "event:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("האירוע נמחק"));
    }

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<Event> BaseQuery() => _db.Events
        .Include(e => e.CreatedBy);
}
