namespace SmartHomeApi.Models;

/// <summary>התראה למשתמש בודד.</summary>
public class Notification : IHouseholdOwned
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    /// <summary>מזהה הישות שההתראה מדברת עליה (אירוע, מתנה, רכב...).</summary>
    public int? RelatedEntityId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>מתי ההתראה אמורה להישלח.</summary>
    public DateTime ScheduledFor { get; set; }
}
