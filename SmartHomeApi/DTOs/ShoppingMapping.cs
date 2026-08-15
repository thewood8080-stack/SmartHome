using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// תרגום פריט קניות ל-DTO. מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// </summary>
public static class ShoppingMapping
{
    public static ShoppingItemDto ToDto(ShoppingItem item) => new(
        item.Id.ToString(),
        item.Id.ToString(),
        item.Name,
        item.Quantity,
        item.Unit,
        item.Category,
        item.IsUrgent,
        item.IsBought,
        ToUserDto(item.AddedBy),
        ToUserDto(item.BoughtBy),
        item.CreatedAt,
        item.BoughtAt);

    private static ShoppingUserDto? ToUserDto(ApplicationUser? user) => user is null
        ? null
        : new ShoppingUserDto(user.Id, user.Id, user.FullName);
}
