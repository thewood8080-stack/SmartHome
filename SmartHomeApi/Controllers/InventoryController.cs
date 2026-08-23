using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// מלאי הבית.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על InventoryItem,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// התראת "מלאי נמוך" מחושבת ב-client מתוך minQuantity ו-quantity — אין כאן
/// מנגנון התראות בצד השרת.
/// </summary>
[ApiController]
[Route("api/inventory")]
[Authorize]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;

    public InventoryController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    /// <summary>רשימת פריטי המלאי של הבית, לפי סדר אלפביתי.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var items = await BaseQuery()
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .ToListAsync();

        return Ok(items.Select(InventoryMapping.ToDto));
    }

    /// <summary>הוספת פריט חדש. פתוח לכל בני הבית.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateInventoryItemRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var item = new InventoryItem
        {
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Quantity = request.Quantity,
            Unit = InventoryMapping.Normalize(request.Unit),
            MinQuantity = request.MinQuantity,
            Location = InventoryMapping.Normalize(request.Location),
            ExpiryDate = request.ExpiryDate,
            Note = InventoryMapping.Normalize(request.Note),
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            AddedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(i => i.Id == item.Id);
        var dto = InventoryMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "inventory:created", dto);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>עדכון מלא של פריט קיים. פתוח לכל בני הבית.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateInventoryItemRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var item = await BaseQuery().FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return NotFound(new MessageResponse("הפריט לא נמצא"));

        item.Name = request.Name.Trim();
        item.Category = request.Category.Trim();
        item.Quantity = request.Quantity;
        item.Unit = InventoryMapping.Normalize(request.Unit);
        item.MinQuantity = request.MinQuantity;
        item.Location = InventoryMapping.Normalize(request.Location);
        item.ExpiryDate = request.ExpiryDate;
        item.Note = InventoryMapping.Normalize(request.Note);

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(i => i.Id == item.Id);
        var dto = InventoryMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "inventory:updated", dto);

        return Ok(dto);
    }

    /// <summary>
    /// עדכון כמות מהיר מכפתורי ה-+/- ברשימה.
    /// משדר inventory:updated כמו PUT — מבחינת שאר הבית זה אותו שינוי,
    /// בדיוק כמו ש-ShoppingController.SetBought משדר shopping:updated.
    /// </summary>
    [HttpPatch("{id:int}/quantity")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuantity(int id, UpdateQuantityRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        // ההגנה הזו קיימת גם ב-client (updateQty יוצא מוקדם על ערך שלילי),
        // אבל היא נאכפת גם כאן כדי שקריאה ישירה ל-API לא תיצור מלאי שלילי.
        if (request.Quantity < 0)
            return BadRequest(new MessageResponse("הכמות לא יכולה להיות שלילית"));

        var item = await BaseQuery().FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return NotFound(new MessageResponse("הפריט לא נמצא"));

        item.Quantity = request.Quantity!.Value;

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(i => i.Id == item.Id);
        var dto = InventoryMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "inventory:updated", dto);

        return Ok(dto);
    }

    /// <summary>
    /// מחיקת פריט. מנהל בלבד — כפתור המחיקה ב-InventoryPage.tsx מוצג רק למנהל,
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

        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return NotFound(new MessageResponse("הפריט לא נמצא"));

        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "inventory:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("הפריט נמחק"));
    }

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<InventoryItem> BaseQuery() => _db.InventoryItems
        .Include(i => i.AddedBy);
}
