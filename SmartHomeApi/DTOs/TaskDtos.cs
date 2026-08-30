using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// משימה כפי ש-TasksPage.tsx קורא אותה.
/// המיפוי מה-enums נעשה כאן בלבד — המודל לא יודע על 'high'/'todo' וכדומה.
/// </summary>
public record TaskDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Title,
    string Description,
    string Priority,
    string Status,
    int Points,
    TaskUserDto? AssignedTo,
    TaskUserDto? CreatedBy,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>משתמש מקוצר בתוך משימה. ה-client קורא _id ו-name.</summary>
public record TaskUserDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Name);

public class CreateTaskRequest
{
    [Required(ErrorMessage = "נדרשת כותרת למשימה")]
    [MaxLength(200, ErrorMessage = "הכותרת ארוכה מדי")]
    public string Title { get; set; } = string.Empty;

    /// <summary>HTML מעורך הטקסט העשיר — מנוקה בשרת לפני שמירה.</summary>
    public string? Description { get; set; }

    /// <summary>'high' | 'medium' | 'low'. ברירת המחדל היא 'medium'.</summary>
    public string? Priority { get; set; }

    /// <summary>מזהה המשתמש האחראי. ריק = משימה לכל בני הבית.</summary>
    public string? AssignedTo { get; set; }

    /// <summary>ה-client שולח מחרוזת ריקה כשלא נבחר תאריך.</summary>
    public string? DueDate { get; set; }

    /// <summary>
    /// סימון ה-checkbox בטופס — האם לשלוח התראת מייל לשאר בני הבית.
    /// נקרא ביצירה בלבד; בעדכון השדה מתקבל ומתעלמים ממנו.
    /// </summary>
    public bool SendEmailNotification { get; set; }
}

/// <summary>עדכון מלא של משימה. אותם שדות כמו ביצירה.</summary>
public class UpdateTaskRequest : CreateTaskRequest;

public class UpdateTaskStatusRequest
{
    [Required(ErrorMessage = "נדרש לציין סטטוס")]
    public string Status { get; set; } = string.Empty;
}
