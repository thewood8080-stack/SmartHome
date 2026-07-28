namespace SmartHomeApi.Models;

/// <summary>משימת בית.</summary>
public class HouseTask : IHouseholdOwned
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>HTML מעורך הטקסט העשיר.</summary>
    public string Description { get; set; } = string.Empty;

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public DateTime? DueDate { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public HouseTaskStatus Status { get; set; } = HouseTaskStatus.Pending;

    public int Points { get; set; }

    public TaskRecurring Recurring { get; set; } = TaskRecurring.None;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
