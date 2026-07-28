namespace SmartHomeApi.Models;

/// <summary>פריט במלאי הבית.</summary>
public class InventoryItem : IHouseholdOwned
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; } = 0;

    public string? Unit { get; set; }

    /// <summary>מתחת לכמות הזו הפריט נחשב חסר.</summary>
    public int MinQuantity { get; set; } = 0;

    public string? Location { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Note { get; set; }

    public string AddedById { get; set; } = string.Empty;
    public ApplicationUser? AddedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
