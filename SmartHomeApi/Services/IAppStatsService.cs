namespace SmartHomeApi.Services;

/// <summary>
/// Application state — נתונים ברמת האפליקציה כולה, משותפים לכל הבקשות.
/// יושב ב-IMemoryCache כ-singleton ומתאפס בכל עליית שרת.
/// </summary>
public interface IAppStatsService
{
    /// <summary>מבקר חדש — נספר פעם אחת לכל סשן ולא בכל בקשה.</summary>
    void TrackVisit();

    /// <summary>מסמן שהמשתמש פעיל עכשיו. נקרא בכל בקשה מזוהה.</summary>
    void TrackActiveUser(string userId);

    /// <summary>מסיר משתמש מרשימת המחוברים — ביציאה מפורשת.</summary>
    void RemoveActiveUser(string userId);

    long TotalVisitors { get; }

    /// <summary>מספר המשתמשים שביצעו בקשה בחלון הפעילות האחרון.</summary>
    int ConnectedUsers { get; }

    /// <summary>מתי השרת עלה — המונים נספרים מהרגע הזה.</summary>
    DateTime SinceUtc { get; }
}
