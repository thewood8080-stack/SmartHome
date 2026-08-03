using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// שורת משתמש ברשימת הניהול.
/// ה-client הקיים מזהה משתמשים לפי _id (שריד מ-MongoDB), ולכן אותו מזהה
/// מוחזר גם כ-_id וגם כ-id — כדי לא לגעת ב-client ולא להנציח את השם הישן בקוד חדש.
/// </summary>
public record UserListItemDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Name,
    string Email,
    string Role,
    bool Approved,
    int Points,
    DateTime CreatedAt);

public class ApproveUserRequest
{
    [Required(ErrorMessage = "נדרש לציין אישור או ביטול")]
    public bool? Approved { get; set; }
}

public class ChangeRoleRequest
{
    [Required(ErrorMessage = "נדרש לציין תפקיד")]
    public string Role { get; set; } = string.Empty;
}
