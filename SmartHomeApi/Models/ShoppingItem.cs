namespace SmartHomeApi.Models;

/// <summary>פריט ברשימת הקניות המשפחתית.</summary>
public class ShoppingItem : IHouseholdOwned
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public string? Unit { get; set; }

    public string? Category { get; set; }

    public bool IsUrgent { get; set; } = false;

    public bool IsBought { get; set; } = false;

    public string AddedById { get; set; } = string.Empty;
    public ApplicationUser? AddedBy { get; set; }

    public string? BoughtById { get; set; }
    public ApplicationUser? BoughtBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? BoughtAt { get; set; }
}
