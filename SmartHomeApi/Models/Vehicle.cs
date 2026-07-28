namespace SmartHomeApi.Models;

/// <summary>רכב משפחתי — טיפולים, ביטוח וטסט.</summary>
public class Vehicle : IHouseholdOwned
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string LicensePlate { get; set; } = string.Empty;

    public string? Model { get; set; }

    public int? Year { get; set; }

    public DateTime? LastService { get; set; }

    public DateTime? NextService { get; set; }

    public DateTime? InsuranceExpiry { get; set; }

    public DateTime? TestExpiry { get; set; }

    public string? FuelType { get; set; }

    public string? Notes { get; set; }

    public string AddedById { get; set; } = string.Empty;
    public ApplicationUser? AddedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
