using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// פריט קניות כפי ש-ShoppingPage.tsx קורא אותו.
/// urgent/bought מקבלים שם מפורש כי ה-client קורא בדיוק לשמות האלה,
/// ולא ל-isUrgent/isBought שהיו נגזרים אוטומטית משמות המאפיינים.
/// </summary>
public record ShoppingItemDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Name,
    int Quantity,
    string? Unit,
    string? Category,
    [property: JsonPropertyName("urgent")] bool IsUrgent,
    [property: JsonPropertyName("bought")] bool IsBought,
    ShoppingUserDto? AddedBy,
    ShoppingUserDto? BoughtBy,
    DateTime CreatedAt,
    DateTime? BoughtAt);

/// <summary>משתמש מקוצר בתוך פריט קניות. ה-client קורא _id ו-name.</summary>
public record ShoppingUserDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Name);

public class CreateShoppingItemRequest
{
    [Required(ErrorMessage = "נדרש שם מוצר")]
    [MaxLength(200, ErrorMessage = "שם המוצר ארוך מדי")]
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    [MaxLength(30, ErrorMessage = "היחידה ארוכה מדי")]
    public string? Unit { get; set; }

    [MaxLength(50, ErrorMessage = "הקטגוריה ארוכה מדי")]
    public string? Category { get; set; }

    /// <summary>ה-client שולח 'urgent', לא 'isUrgent'.</summary>
    [JsonPropertyName("urgent")]
    public bool IsUrgent { get; set; }
}

/// <summary>עדכון מלא של פריט. אותם שדות כמו ביצירה.</summary>
public class UpdateShoppingItemRequest : CreateShoppingItemRequest;

/// <summary>סימון 'נקנה' / ביטול הסימון.</summary>
public class UpdateBoughtRequest
{
    /// <summary>ה-client שולח { "bought": true }.</summary>
    [JsonPropertyName("bought")]
    public bool IsBought { get; set; }
}
