namespace SmartHomeApi.DTOs;

/// <summary>נתוני Application state — נשמרים ב-IMemoryCache ולא בבסיס הנתונים.</summary>
/// <param name="TotalVisitors">מונה מבקרים כולל מאז עליית השרת.</param>
/// <param name="ConnectedUsers">משתמשים מחוברים — מי שביצע בקשה בדקות האחרונות.</param>
/// <param name="SinceUtc">מתי המונים אופסו, כלומר מתי השרת עלה.</param>
public record StatsDto(
    long TotalVisitors,
    int ConnectedUsers,
    DateTime SinceUtc);
