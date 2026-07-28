using Microsoft.AspNetCore.Identity;

namespace SmartHomeApi.Models;

/// <summary>משתמש המערכת — מרחיב את IdentityUser בשדות המשפחתיים.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public int Points { get; set; } = 0;

    /// <summary>משתמש חדש ממתין לאישור מנהל לפני שהוא יכול להשתמש במערכת.</summary>
    public bool IsApproved { get; set; } = false;

    public bool IsManager { get; set; } = false;

    /// <summary>null = המשתמש עדיין לא שויך למשק בית.</summary>
    public int? HouseholdId { get; set; }
    public Household? Household { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
