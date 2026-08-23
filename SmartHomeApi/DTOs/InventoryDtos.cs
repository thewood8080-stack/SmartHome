using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// פריט מלאי כפי ש-InventoryPage.tsx קורא אותו.
/// הדף מקבץ את הרשימה לפי category, ולכן השדה חייב לחזור תמיד ולא להיות null.
/// </summary>
public record InventoryItemDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Name,
    string Category,
    int Quantity,
    string? Unit,
    int MinQuantity,
    string? Location,
    DateTime? ExpiryDate,
    string? Note,
    InventoryUserDto? AddedBy,
    DateTime CreatedAt);

/// <summary>מי הוסיף את הפריט. ה-client קורא name בלבד.</summary>
public record InventoryUserDto(string Name);

public class CreateInventoryItemRequest
{
    [Required(ErrorMessage = "נדרש שם פריט")]
    [MaxLength(200, ErrorMessage = "שם הפריט ארוך מדי")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרשת קטגוריה")]
    [MaxLength(50, ErrorMessage = "הקטגוריה ארוכה מדי")]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "הכמות לא יכולה להיות שלילית")]
    public int Quantity { get; set; }

    [MaxLength(30, ErrorMessage = "היחידה ארוכה מדי")]
    public string? Unit { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "כמות המינימום לא יכולה להיות שלילית")]
    public int MinQuantity { get; set; }

    [MaxLength(100, ErrorMessage = "המיקום ארוך מדי")]
    public string? Location { get; set; }

    public DateTime? ExpiryDate { get; set; }

    /// <summary>ה-client לא שולח הערה כרגע; השדה קיים במודל ונשאר ריק.</summary>
    [MaxLength(500, ErrorMessage = "ההערה ארוכה מדי")]
    public string? Note { get; set; }
}

/// <summary>עדכון מלא של פריט. אותם שדות כמו ביצירה.</summary>
public class UpdateInventoryItemRequest : CreateInventoryItemRequest;

/// <summary>עדכון כמות מהיר מכפתורי ה-+/- ברשימה.</summary>
public class UpdateQuantityRequest
{
    /// <summary>
    /// nullable כדי ש-[Required] יזהה בקשה בלי כמות ויחזיר 400,
    /// במקום ש-int ריק ייפול בשקט ל-0 ויאפס את המלאי.
    /// </summary>
    [Required(ErrorMessage = "נדרשת כמות")]
    [Range(0, int.MaxValue, ErrorMessage = "הכמות לא יכולה להיות שלילית")]
    public int? Quantity { get; set; }
}
