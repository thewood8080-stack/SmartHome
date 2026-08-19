using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// מתנה כפי ש-GiftsPage.tsx קורא אותה.
/// שים לב: אין כאן שדה סכום/מחיר — לא במודל, לא ב-DTO ולא בממשק. זה כלל מוחלט בפרויקט.
/// purchased מקבל שם מפורש כי ה-client קורא בדיוק לשם הזה ולא ל-isPurchased.
/// </summary>
public record GiftDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string RecipientName,
    string Occasion,
    DateTime Date,
    IReadOnlyList<string> Ideas,
    [property: JsonPropertyName("purchased")] bool IsPurchased,
    string? PurchasedItem,
    string? Note,
    // מחרוזת כמו שאר ה-id-ים שה-client מקבל, null כשאין קישור לאירוע.
    string? EventId,
    GiftEventDto? Event,
    DateTime CreatedAt);

/// <summary>
/// האירוע המקושר בגרסה מקוצרת — בדיוק מה שנדרש כדי להציג
/// "מקושר לאירוע: &lt;כותרת&gt;, &lt;תאריך&gt;" מתחת לפרטי המתנה.
/// </summary>
public record GiftEventDto(
    string Id,
    string Title,
    [property: JsonPropertyName("date")] DateTime StartDate);

public class CreateGiftRequest
{
    [Required(ErrorMessage = "נדרש שם המקבל")]
    [MaxLength(100, ErrorMessage = "שם המקבל ארוך מדי")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרש שם האירוע")]
    [MaxLength(100, ErrorMessage = "שם האירוע ארוך מדי")]
    public string Occasion { get; set; } = string.Empty;

    /// <summary>nullable כדי ש-[Required] יזהה בקשה בלי תאריך ויחזיר 400.</summary>
    [Required(ErrorMessage = "נדרש תאריך האירוע")]
    public DateTime? Date { get; set; }

    /// <summary>ה-client שולח מערך; המודל שומר מחרוזת אחת מופרדת ב-" | ".</summary>
    public List<string>? Ideas { get; set; }

    [MaxLength(500, ErrorMessage = "ההערה ארוכה מדי")]
    public string? Note { get; set; }

    /// <summary>קישור אופציונלי לאירוע בלוח השנה.</summary>
    public int? EventId { get; set; }
}

/// <summary>
/// עדכון מתנה — <b>חלקי</b>, בשונה מ-UpdateShoppingItemRequest שהוא עדכון מלא.
/// הסיבה: GiftsPage.tsx שולח ב-markPurchased רק { purchased, purchasedItem },
/// ועדכון גורף היה מאפס בבקשה כזו את שם המקבל, האירוע, התאריך והרעיונות.
/// לכן כל שדה כאן nullable, והקונטרולר מעדכן property-by-property רק את מה שהגיע.
/// המשמעות הנלווית: אי אפשר לנקות שדה בשליחת null — ניקוי דורש שדה ייעודי,
/// ואין לו צורך בממשק הנוכחי.
/// </summary>
public class UpdateGiftRequest
{
    [MaxLength(100, ErrorMessage = "שם המקבל ארוך מדי")]
    public string? RecipientName { get; set; }

    [MaxLength(100, ErrorMessage = "שם האירוע ארוך מדי")]
    public string? Occasion { get; set; }

    public DateTime? Date { get; set; }

    public List<string>? Ideas { get; set; }

    /// <summary>ה-client שולח { "purchased": true }.</summary>
    [JsonPropertyName("purchased")]
    public bool? IsPurchased { get; set; }

    [MaxLength(200, ErrorMessage = "תיאור הפריט שנקנה ארוך מדי")]
    public string? PurchasedItem { get; set; }

    [MaxLength(500, ErrorMessage = "ההערה ארוכה מדי")]
    public string? Note { get; set; }

    public int? EventId { get; set; }
}
