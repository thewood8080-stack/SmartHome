using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// רשימת הקניות המשפחתית.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על ShoppingItem,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// </summary>
[ApiController]
[Route("api/shopping")]
[Authorize]
[Produces("application/json")]
public class ShoppingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;
    private readonly IEmailService _email;
    private readonly ILogger<ShoppingController> _logger;

    public ShoppingController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier,
        IEmailService email,
        ILogger<ShoppingController> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _email = email;
        _logger = logger;
    }

    /// <summary>רשימת הפריטים של הבית. החדשים למעלה — כמו סדר ההוספה בזמן אמת.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ShoppingItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var items = await BaseQuery()
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(ShoppingMapping.ToDto));
    }

    /// <summary>הוספת פריט חדש לרשימה.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShoppingItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateShoppingItemRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var item = new ShoppingItem
        {
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            Unit = Normalize(request.Unit),
            Category = Normalize(request.Category),
            IsUrgent = request.IsUrgent,
            IsBought = false,
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            AddedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.ShoppingItems.Add(item);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(i => i.Id == item.Id);
        var dto = ShoppingMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "shopping:created", dto);

        // רק אם המשתמש סימן את התיבה בטופס. השמירה כבר הסתיימה בהצלחה בשלב הזה.
        if (request.SendEmailNotification)
            await SendCreationEmailsAsync(householdId.Value, new NewItemNotification(
                "פריט קניות חדש",
                created.Name,
                created.AddedBy?.FullName ?? _currentUser.FullName ?? "בן בית"));

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>עדכון פרטי פריט קיים.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ShoppingItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateShoppingItemRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var item = await BaseQuery().FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return NotFound(new MessageResponse("הפריט לא נמצא"));

        item.Name = request.Name.Trim();
        item.Quantity = request.Quantity;
        item.Unit = Normalize(request.Unit);
        item.Category = Normalize(request.Category);
        item.IsUrgent = request.IsUrgent;

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(i => i.Id == item.Id);
        var dto = ShoppingMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "shopping:updated", dto);

        return Ok(dto);
    }

    /// <summary>סימון 'נקנה' או ביטול הסימון. נשמר מי סימן ומתי.</summary>
    [HttpPatch("{id:int}/bought")]
    [ProducesResponseType(typeof(ShoppingItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetBought(int id, UpdateBoughtRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var item = await BaseQuery().FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return NotFound(new MessageResponse("הפריט לא נמצא"));

        item.IsBought = request.IsBought;

        if (request.IsBought)
        {
            item.BoughtById = _currentUser.UserId;
            item.BoughtAt = DateTime.UtcNow;
        }
        else
        {
            // ביטול הסימון מנקה גם את מי שקנה ומתי.
            item.BoughtById = null;
            item.BoughtAt = null;
        }

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(i => i.Id == item.Id);
        var dto = ShoppingMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "shopping:updated", dto);

        return Ok(dto);
    }

    /// <summary>ניקוי כל הפריטים שכבר נקנו.</summary>
    /// <remarks>
    /// הראוט הוא clear/bought ולא clear-bought כי זו הכתובת ש-ShoppingPage.tsx קורא לה בפועל.
    /// אין התנגשות עם {id:int} — מספר הסגמנטים שונה.
    /// </remarks>
    [HttpDelete("clear/bought")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearBought()
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var bought = await _db.ShoppingItems.Where(i => i.IsBought).ToListAsync();

        _db.ShoppingItems.RemoveRange(bought);
        await _db.SaveChangesAsync();

        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "shopping:cleared", new { count = bought.Count });

        return Ok(new MessageResponse($"נמחקו {bought.Count} פריטים שנקנו"));
    }

    /// <summary>מחיקת פריט בודד.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var item = await _db.ShoppingItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return NotFound(new MessageResponse("הפריט לא נמצא"));

        _db.ShoppingItems.Remove(item);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "shopping:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("הפריט נמחק"));
    }

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<ShoppingItem> BaseQuery() => _db.ShoppingItems
        .Include(i => i.AddedBy)
        .Include(i => i.BoughtBy);

    /// <summary>
    /// שולח את התראת המייל לכל בני הבית חוץ מהמוסיף עצמו.
    /// כישלון שליחה נרשם ללוג בלבד ולא מפיל את הוספת הפריט — אותו דפוס
    /// כמו מייל איפוס הסיסמה ב-AuthController.
    /// </summary>
    private async Task SendCreationEmailsAsync(int householdId, NewItemNotification notification)
    {
        var creatorId = _currentUser.UserId;

        // ApplicationUser אינו נכלל בפילטר הגלובלי, ולכן הסינון לפי משק בית נעשה כאן במפורש.
        var recipients = await _db.Users
            .AsNoTracking()
            .Where(u => u.HouseholdId == householdId
                        && u.Id != creatorId
                        && u.Email != null
                        && u.Email != string.Empty)
            .Select(u => new { u.Email, u.FullName })
            .ToListAsync();

        foreach (var recipient in recipients)
        {
            try
            {
                await _email.SendNewItemNotificationAsync(recipient.Email!, recipient.FullName, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שליחת התראת מייל על פריט קניות חדש נכשלה עבור {Email}", recipient.Email);
            }
        }
    }

    /// <summary>ה-client שולח מחרוזת ריקה כשהשדה לא מולא.</summary>
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
