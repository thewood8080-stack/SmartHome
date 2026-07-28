namespace SmartHomeApi.Services;

/// <summary>
/// מספק את זהות המשתמש המחובר ואת משק הבית שלו ל-ApplicationDbContext,
/// כדי שה-Global Query Filters יסננו אוטומטית לפי משק הבית.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }

    /// <summary>null כשאין משתמש מחובר — ואז הפילטרים לא מחזירים כלום (fail closed).</summary>
    int? HouseholdId { get; }

    /// <summary>נכתב ב-Login ונקרא בכל בקשה אחרי כן.</summary>
    void SetHousehold(int? householdId);
}
