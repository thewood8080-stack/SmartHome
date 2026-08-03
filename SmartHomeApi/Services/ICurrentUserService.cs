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

    /// <summary>
    /// שם המשתמש המחובר, לרישום ביומן הפעולות.
    /// נשמר ב-Session בכניסה כדי לא לשלוף את המשתמש מבסיס הנתונים בכל בקשה.
    /// </summary>
    string? FullName { get; }

    /// <summary>האם המשתמש הוא מנהל הבית.</summary>
    bool IsManager { get; }

    /// <summary>נכתב ב-Login ונקרא בכל בקשה אחרי כן.</summary>
    void SetHousehold(int? householdId);

    /// <summary>נכתב ב-Login יחד עם משק הבית.</summary>
    void SetFullName(string? fullName);

    /// <summary>מנקה את נתוני הסשן ביציאה.</summary>
    void Clear();
}
