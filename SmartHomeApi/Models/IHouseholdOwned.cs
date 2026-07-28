namespace SmartHomeApi.Models;

/// <summary>
/// כל ישות ששייכת למשק בית. ApplicationDbContext מחיל Global Query Filter
/// על כל מי שמממש את הממשק הזה — כך אי אפשר לשכוח לסנן ישות חדשה.
/// </summary>
public interface IHouseholdOwned
{
    int HouseholdId { get; set; }
}
