using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// תרגום פריט מלאי ל-DTO. מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// </summary>
public static class InventoryMapping
{
    public static InventoryItemDto ToDto(InventoryItem item) => new(
        item.Id.ToString(),
        item.Id.ToString(),
        item.Name,
        item.Category,
        item.Quantity,
        item.Unit,
        item.MinQuantity,
        item.Location,
        item.ExpiryDate,
        item.Note,
        ToUserDto(item.AddedBy),
        item.CreatedAt);

    private static InventoryUserDto? ToUserDto(ApplicationUser? user) => user is null
        ? null
        : new InventoryUserDto(user.FullName);

    /// <summary>ה-client שולח מחרוזת ריקה כשהשדה לא מולא.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
