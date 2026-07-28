namespace SmartHomeApi.Models;

/// <summary>רשומה רפואית של בן משפחה.</summary>
public class MedicalRecord : IHouseholdOwned
{
    public int Id { get; set; }

    /// <summary>בן המשפחה שהרשומה שייכת לו.</summary>
    public string MemberId { get; set; } = string.Empty;
    public ApplicationUser? Member { get; set; }

    public MedicalRecordType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string? Doctor { get; set; }

    public string? Clinic { get; set; }

    public string? Notes { get; set; }

    public DateTime? NextAppointment { get; set; }

    public string AddedById { get; set; } = string.Empty;
    public ApplicationUser? AddedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
