namespace SmartHomeApi.Models;

/// <summary>אירוע בלוח השנה המשפחתי.</summary>
public class Event : IHouseholdOwned
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>HTML מעורך הטקסט העשיר.</summary>
    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool AllDay { get; set; } = false;

    public string Color { get; set; } = "#1E3A5F";

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    /// <summary>כמה דקות לפני האירוע לשלוח תזכורת.</summary>
    public int ReminderMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
