using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// ניהול התקציב המשפחתי — הכנסות והוצאות.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על BudgetEntry,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// כל בני הבית רשאים להוסיף, לעדכן ולמחוק — אין כאן הגבלת מנהל,
/// בהתאמה ל-BudgetPage.tsx שאינו בודק תפקיד באף פעולה.
/// </summary>
[ApiController]
[Route("api/budget")]
[Authorize]
[Produces("application/json")]
public class BudgetController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;

    public BudgetController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    /// <summary>
    /// רשומות התקציב של הבית, החדשות למעלה.
    /// month ו-year מסננים לחודש מסוים ונשלחים תמיד יחד; בלעדיהם מוחזר הכל.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BudgetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int? month, [FromQuery] int? year)
    {
        var query = BaseQuery().AsNoTracking();

        if (month is not null || year is not null)
        {
            if (month is null || year is null)
                return BadRequest(new MessageResponse("יש לשלוח את החודש והשנה יחד"));

            if (month is < 1 or > 12)
                return BadRequest(new MessageResponse("החודש חייב להיות בין 1 ל-12"));

            // טווח חצי פתוח: מתחילת החודש ועד תחילת החודש הבא. כך כל השעות
            // ביום האחרון נכללות, בלי להסתמך על "סוף היום" כערך קבוע.
            var from = new DateTime(year.Value, month.Value, 1);
            var to = from.AddMonths(1);

            query = query.Where(b => b.Date >= from && b.Date < to);
        }

        var entries = await query
            .OrderByDescending(b => b.Date)
            .ToListAsync();

        return Ok(entries.Select(BudgetMapping.ToDto));
    }

    /// <summary>הוספת תנועה חדשה.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateBudgetRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!BudgetMapping.TryParseType(request.Type, out var type))
            return BadRequest(new MessageResponse("סוג התנועה אינו מוכר"));

        var entry = new BudgetEntry
        {
            Title = request.Title.Trim(),
            Amount = request.Amount!.Value,
            Type = type,
            Category = request.Category.Trim(),
            Date = request.Date!.Value,
            Note = BudgetMapping.Normalize(request.Note),
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            AddedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.BudgetEntries.Add(entry);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(b => b.Id == entry.Id);
        var dto = BudgetMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "budget:created", dto);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>עדכון מלא של תנועה קיימת.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateBudgetRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var entry = await BaseQuery().FirstOrDefaultAsync(b => b.Id == id);
        if (entry is null)
            return NotFound(new MessageResponse("הרשומה לא נמצאה"));

        if (!BudgetMapping.TryParseType(request.Type, out var type))
            return BadRequest(new MessageResponse("סוג התנועה אינו מוכר"));

        entry.Title = request.Title.Trim();
        entry.Amount = request.Amount!.Value;
        entry.Type = type;
        entry.Category = request.Category.Trim();
        entry.Date = request.Date!.Value;
        entry.Note = BudgetMapping.Normalize(request.Note);

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(b => b.Id == entry.Id);
        var dto = BudgetMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "budget:updated", dto);

        return Ok(dto);
    }

    /// <summary>מחיקת תנועה.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var entry = await _db.BudgetEntries.FirstOrDefaultAsync(b => b.Id == id);
        if (entry is null)
            return NotFound(new MessageResponse("הרשומה לא נמצאה"));

        _db.BudgetEntries.Remove(entry);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "budget:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("הרשומה נמחקה"));
    }

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<BudgetEntry> BaseQuery() => _db.BudgetEntries
        .Include(b => b.AddedBy);
}
