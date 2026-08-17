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
/// משימות בית.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על HouseTask,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IRealtimeNotifier _notifier;

    public TasksController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IHtmlSanitizerService sanitizer,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _notifier = notifier;
    }

    /// <summary>רשימת המשימות של הבית, עם סינון אופציונלי.</summary>
    /// <param name="status">'todo' | 'inprogress' | 'done'</param>
    /// <param name="assignedTo">מזהה המשתמש האחראי</param>
    /// <param name="priority">'high' | 'medium' | 'low'</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? assignedTo,
        [FromQuery] string? priority)
    {
        var query = BaseQuery().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TaskMapping.TryParseStatus(status, out var parsedStatus))
                return BadRequest(new MessageResponse("סטטוס לא מוכר"));

            query = query.Where(t => t.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            if (!TaskMapping.TryParsePriority(priority, out var parsedPriority))
                return BadRequest(new MessageResponse("עדיפות לא מוכרת"));

            query = query.Where(t => t.Priority == parsedPriority);
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            var assignedToId = assignedTo.Trim();
            query = query.Where(t => t.AssignedToId == assignedToId);
        }

        // הפתוחות קודם, ובתוך כל קבוצה החדשות למעלה — כמו שהרשימה מוצגת.
        var tasks = await query
            .OrderBy(t => t.Status == HouseTaskStatus.Done)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks.Select(TaskMapping.ToDto));
    }

    /// <summary>יצירת משימה חדשה.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!TaskMapping.TryParsePriority(request.Priority, out var priority))
            return BadRequest(new MessageResponse("עדיפות לא מוכרת"));

        var (dueDate, dueDateError) = ParseDueDate(request.DueDate);
        if (dueDateError is not null)
            return BadRequest(new MessageResponse(dueDateError));

        var (assignedToId, assignError) = await ResolveAssigneeAsync(request.AssignedTo, householdId.Value);
        if (assignError is not null)
            return BadRequest(new MessageResponse(assignError));

        var task = new HouseTask
        {
            Title = request.Title.Trim(),
            // ניקוי ה-HTML לפני השמירה — זו ההגנה מפני XSS.
            Description = _sanitizer.Sanitize(request.Description),
            Priority = priority,
            Points = TaskMapping.PointsFor(priority),
            Status = HouseTaskStatus.Pending,
            AssignedToId = assignedToId,
            DueDate = dueDate,
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            CreatedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.HouseTasks.Add(task);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(t => t.Id == task.Id);
        var dto = TaskMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "task:created", dto);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>עדכון משימה קיימת.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var task = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
            return NotFound(new MessageResponse("המשימה לא נמצאה"));

        if (!TaskMapping.TryParsePriority(request.Priority, out var priority))
            return BadRequest(new MessageResponse("עדיפות לא מוכרת"));

        var (dueDate, dueDateError) = ParseDueDate(request.DueDate);
        if (dueDateError is not null)
            return BadRequest(new MessageResponse(dueDateError));

        var (assignedToId, assignError) = await ResolveAssigneeAsync(request.AssignedTo, householdId.Value);
        if (assignError is not null)
            return BadRequest(new MessageResponse(assignError));

        task.Title = request.Title.Trim();
        task.Description = _sanitizer.Sanitize(request.Description);
        task.Priority = priority;
        // הניקוד תמיד נגזר מהעדיפות, גם בעדכון.
        task.Points = TaskMapping.PointsFor(priority);
        task.AssignedToId = assignedToId;
        task.DueDate = dueDate;

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(t => t.Id == task.Id);
        var dto = TaskMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "task:updated", dto);

        return Ok(dto);
    }

    /// <summary>שינוי סטטוס. במעבר ל'בוצע' הנקודות נזקפות למי שביצע את הפעולה.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateTaskStatusRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!TaskMapping.TryParseStatus(request.Status, out var newStatus))
            return BadRequest(new MessageResponse("סטטוס לא מוכר"));

        var task = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
            return NotFound(new MessageResponse("המשימה לא נמצאה"));

        var wasDone = task.Status == HouseTaskStatus.Done;
        task.Status = newStatus;

        if (newStatus == HouseTaskStatus.Done)
        {
            // רק במעבר אמיתי ל'בוצע' — סימון חוזר לא מזכה בנקודות פעמיים.
            if (!wasDone)
            {
                task.CompletedAt = DateTime.UtcNow;

                var performer = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId);
                if (performer is not null)
                    performer.Points += task.Points;
            }
        }
        else
        {
            // ביטול 'בוצע' מאפס את מועד הסיום; הנקודות שכבר נזקפו נשארות.
            task.CompletedAt = null;
        }

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(t => t.Id == task.Id);
        var dto = TaskMapping.ToDto(updated);

        // כאן משתנות גם הנקודות של המבצע, ולכן חשוב שכל בני הבית יקבלו את העדכון.
        await _notifier.NotifyHouseholdAsync(householdId.Value, "task:updated", dto);

        return Ok(dto);
    }

    /// <summary>מחיקת משימה.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var task = await _db.HouseTasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
            return NotFound(new MessageResponse("המשימה לא נמצאה"));

        _db.HouseTasks.Remove(task);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "task:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("המשימה נמחקה"));
    }

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<HouseTask> BaseQuery() => _db.HouseTasks
        .Include(t => t.AssignedTo)
        .Include(t => t.CreatedBy);

    /// <summary>ה-client שולח מחרוזת ריקה כשלא נבחר תאריך.</summary>
    private static (DateTime? Value, string? Error) ParseDueDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, null);

        var parsed = DateTime.TryParse(
            raw.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var value);

        return parsed ? (value, null) : (null, "תאריך היעד אינו תקין");
    }

    /// <summary>
    /// מוודא שהאחראי הוא חבר באותו משק בית.
    /// בלי הבדיקה הזו אפשר היה לשייך משימה למשתמש מבית אחר ולדלוף את שמו.
    /// </summary>
    private async Task<(string? Id, string? Error)> ResolveAssigneeAsync(string? assignedTo, int householdId)
    {
        if (string.IsNullOrWhiteSpace(assignedTo))
            return (null, null);

        var id = assignedTo.Trim();
        var exists = await _db.Users.AnyAsync(u => u.Id == id && u.HouseholdId == householdId);

        return exists ? (id, null) : (null, "המשתמש שנבחר אינו חבר בבית הזה");
    }
}
