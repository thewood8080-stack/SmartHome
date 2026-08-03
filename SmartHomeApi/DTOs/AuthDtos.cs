using System.ComponentModel.DataAnnotations;

namespace SmartHomeApi.DTOs;

/// <summary>
/// הרשמה. שני מסלולים: יצירת בית חדש (HouseholdName) או הצטרפות לבית קיים (InviteCode).
/// אם שניהם ריקים — זו שגיאה.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "נדרש שם מלא")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרשת כתובת מייל")]
    [EmailAddress(ErrorMessage = "כתובת מייל לא תקינה")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרשת סיסמה")]
    public string Password { get; set; } = string.Empty;

    /// <summary>שם הבית — במסלול יצירת בית חדש.</summary>
    public string? HouseholdName { get; set; }

    /// <summary>קוד הצטרפות — במסלול הצטרפות לבית קיים.</summary>
    public string? InviteCode { get; set; }
}

public class LoginRequest
{
    [Required(ErrorMessage = "נדרשת כתובת מייל")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרשת סיסמה")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// פרטי המשתמש כפי שה-client מצפה להם: id, name, email, role, approved, points.
/// role הוא 'admin' או 'member' — ולא שמות התפקידים של Identity.
/// </summary>
public record UserDto(
    string Id,
    string Name,
    string Email,
    string Role,
    bool Approved,
    int Points);

public record HouseholdDto(
    string Id,
    string Name,
    string InviteCode);

/// <summary>
/// תשובת ההתחברות/הרשמה. המבנה נעול מול ה-client הקיים.
/// Token אינו אמצעי האימות — האימות הוא עוגיית ההזדהות והסשן.
/// זהו מזהה הסשן, שנשמר אצל ה-client כדי לשמור על המבנה שהוא מצפה לו.
/// </summary>
public record AuthResponse(
    string Token,
    UserDto User,
    HouseholdDto? Household);
