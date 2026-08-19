namespace SmartHomeApi.Models;

/// <summary>
/// רעיון למתנה. שים לב: אין ולא יהיה כאן שדה סכום/מחיר — לא במודל, לא ב-DTO ולא בממשק.
/// </summary>
public class Gift : IHouseholdOwned
{
    public int Id { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string Occasion { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    /// <summary>רשימת רעיונות כטקסט חופשי.</summary>
    public string Ideas { get; set; } = string.Empty;

    public bool IsPurchased { get; set; } = false;

    /// <summary>
    /// מה נקנה בפועל, כפי שנרשם בסימון 'נקנה'.
    /// שדה נפרד מ-Note בכוונה — הממשק מציג את השניים בשורות שונות.
    /// </summary>
    public string? PurchasedItem { get; set; }

    public string? Note { get; set; }

    /// <summary>קישור אופציונלי לאירוע בלוח השנה.</summary>
    public int? EventId { get; set; }
    public Event? Event { get; set; }

    public string AddedById { get; set; } = string.Empty;
    public ApplicationUser? AddedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
