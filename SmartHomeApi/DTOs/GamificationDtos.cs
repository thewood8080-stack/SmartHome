using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// שורה בלוח המובילים.
/// ה-client מזהה משתמשים לפי _id (שריד מ-MongoDB), ולכן המזהה מוחזר בשם הזה —
/// אותה תבנית כמו UserListItemDto, כדי לא לגעת ב-client.
/// photoURL נגזר מ-ProfileImageUrl שעל המשתמש, ונשאר null כשאין תמונה.
/// </summary>
public record LeaderboardEntryDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Name,
    int Points,
    string Role,
    [property: JsonPropertyName("photoURL")] string? PhotoUrl);

/// <summary>
/// גוף השידור של עדכון נקודות בזמן אמת.
/// id הוא מזהה המשתמש שקיבל את הנקודות, points הוא הסכום המעודכן שלו.
/// </summary>
public record PointsUpdatedDto(string Id, int Points);
