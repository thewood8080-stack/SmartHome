using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// מעקב מתנות, כולל קישור אופציונלי לאירוע בלוח השנה.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על Gift,
/// ולכן כל שאילתה כאן כבר מוגבלת אוטומטית למשק הבית של המשתמש המחובר.
/// כל פעולה משדרת גם עדכון בזמן אמת לשאר בני הבית.
/// אין ולא יהיה כאן שדה סכום או המלצת מחיר — לא בבקשה ולא בתשובה.
/// </summary>
[ApiController]
[Route("api/gifts")]
[Authorize]
[Produces("application/json")]
public class GiftsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;

    public GiftsController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    /// <summary>רשימת המתנות של הבית, לפי סדר התאריכים.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GiftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var gifts = await BaseQuery()
            .AsNoTracking()
            .OrderBy(g => g.Date)
            .ToListAsync();

        return Ok(gifts.Select(GiftMapping.ToDto));
    }

    /// <summary>הוספת מתנה חדשה. פתוח לכל בני הבית.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GiftDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateGiftRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!await EventExistsAsync(request.EventId))
            return BadRequest(new MessageResponse("האירוע המקושר לא נמצא"));

        var gift = new Gift
        {
            RecipientName = request.RecipientName.Trim(),
            Occasion = request.Occasion.Trim(),
            Date = request.Date!.Value,
            Ideas = GiftMapping.JoinIdeas(request.Ideas),
            IsPurchased = false,
            Note = GiftMapping.Normalize(request.Note),
            EventId = request.EventId,
            HouseholdId = householdId.Value,
            // מי ביצע + מתי — נשמר על הישות עצמה.
            AddedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _db.Gifts.Add(gift);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(g => g.Id == gift.Id);
        var dto = GiftMapping.ToDto(created);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "gift:created", dto);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>
    /// עדכון מתנה. <b>חלקי בכוונה</b> — מתעדכן רק מה שנשלח בפועל.
    /// GiftsPage.tsx שולח בסימון 'נקנה' רק { purchased, purchasedItem },
    /// ועדכון גורף כמו ב-ShoppingController היה מאפס בבקשה כזו את שאר השדות.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateGiftRequest request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var gift = await BaseQuery().FirstOrDefaultAsync(g => g.Id == id);
        if (gift is null)
            return NotFound(new MessageResponse("המתנה לא נמצאה"));

        if (!await EventExistsAsync(request.EventId))
            return BadRequest(new MessageResponse("האירוע המקושר לא נמצא"));

        // שדה שלא הגיע בבקשה נשאר כפי שהוא.
        if (request.RecipientName is not null)
            gift.RecipientName = request.RecipientName.Trim();

        if (request.Occasion is not null)
            gift.Occasion = request.Occasion.Trim();

        if (request.Date is not null)
            gift.Date = request.Date.Value;

        if (request.Ideas is not null)
            gift.Ideas = GiftMapping.JoinIdeas(request.Ideas);

        if (request.IsPurchased is not null)
            gift.IsPurchased = request.IsPurchased.Value;

        if (request.PurchasedItem is not null)
            gift.PurchasedItem = GiftMapping.Normalize(request.PurchasedItem);

        if (request.Note is not null)
            gift.Note = GiftMapping.Normalize(request.Note);

        if (request.EventId is not null)
            gift.EventId = request.EventId;

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(g => g.Id == gift.Id);
        var dto = GiftMapping.ToDto(updated);

        await _notifier.NotifyHouseholdAsync(householdId.Value, "gift:updated", dto);

        return Ok(dto);
    }

    /// <summary>
    /// מחיקת מתנה. מנהל בלבד — כפתור המחיקה ב-GiftsPage.tsx מוצג רק למנהל,
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

        var gift = await _db.Gifts.FirstOrDefaultAsync(g => g.Id == id);
        if (gift is null)
            return NotFound(new MessageResponse("המתנה לא נמצאה"));

        _db.Gifts.Remove(gift);
        await _db.SaveChangesAsync();

        // ה-client מזהה את השורה למחיקה לפי _id, שהוא מחרוזת.
        await _notifier.NotifyHouseholdAsync(
            householdId.Value, "gift:deleted", new { id = id.ToString() });

        return Ok(new MessageResponse("המתנה נמחקה"));
    }

    /// <summary>
    /// בדיקה שהאירוע המקושר קיים. הפילטר הגלובלי מגביל את החיפוש למשק הבית המחובר,
    /// ולכן קישור לאירוע של בית אחר נופל כאן ולא מגיע לבסיס הנתונים.
    /// eventId ריק הוא מצב תקין — מתנה בלי קישור.
    /// </summary>
    private async Task<bool> EventExistsAsync(int? eventId) =>
        eventId is null || await _db.Events.AnyAsync(e => e.Id == eventId.Value);

    /// <summary>
    /// שאילתת הבסיס. הפילטר הגלובלי כבר מגביל למשק הבית המחובר,
    /// ולכן אין כאן סינון ידני לפי HouseholdId.
    /// </summary>
    private IQueryable<Gift> BaseQuery() => _db.Gifts
        .Include(g => g.AddedBy)
        .Include(g => g.Event);
}
