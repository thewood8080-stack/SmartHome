namespace SmartHomeApi.Models;

/// <summary>תנועה בתקציב המשפחתי — הכנסה או הוצאה.</summary>
public class BudgetEntry : IHouseholdOwned
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public BudgetType Type { get; set; }

    public string Category { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string? Note { get; set; }

    public string AddedById { get; set; } = string.Empty;
    public ApplicationUser? AddedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
