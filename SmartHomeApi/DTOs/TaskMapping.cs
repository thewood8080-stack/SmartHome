using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// כל התרגום בין ה-enums של המודל למחרוזות שה-client מכיר.
/// מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// </summary>
public static class TaskMapping
{
    private static readonly Dictionary<TaskPriority, string> PriorityToClient = new()
    {
        [TaskPriority.Urgent] = "high",
        [TaskPriority.Normal] = "medium",
        [TaskPriority.CanWait] = "low"
    };

    private static readonly Dictionary<string, TaskPriority> ClientToPriority =
        PriorityToClient.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<HouseTaskStatus, string> StatusToClient = new()
    {
        [HouseTaskStatus.Pending] = "todo",
        [HouseTaskStatus.InProgress] = "inprogress",
        [HouseTaskStatus.Done] = "done"
    };

    private static readonly Dictionary<string, HouseTaskStatus> ClientToStatus =
        StatusToClient.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>הנקודות נגזרות מהעדיפות — הערך המספרי של ה-enum הוא הניקוד.</summary>
    public static int PointsFor(TaskPriority priority) => (int)priority;

    public static string ToClientPriority(TaskPriority priority) => PriorityToClient[priority];

    public static string ToClientStatus(HouseTaskStatus status) => StatusToClient[status];

    /// <summary>מתרגם 'high'/'medium'/'low'. מחרוזת ריקה נופלת לברירת המחדל.</summary>
    public static bool TryParsePriority(string? value, out TaskPriority priority)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            priority = TaskPriority.Normal;
            return true;
        }

        return ClientToPriority.TryGetValue(value.Trim(), out priority);
    }

    /// <summary>מתרגם 'todo'/'inprogress'/'done'.</summary>
    public static bool TryParseStatus(string? value, out HouseTaskStatus status)
    {
        status = HouseTaskStatus.Pending;
        return !string.IsNullOrWhiteSpace(value)
               && ClientToStatus.TryGetValue(value.Trim(), out status);
    }

    public static TaskDto ToDto(HouseTask task) => new(
        task.Id.ToString(),
        task.Id.ToString(),
        task.Title,
        task.Description,
        ToClientPriority(task.Priority),
        ToClientStatus(task.Status),
        task.Points,
        ToUserDto(task.AssignedTo),
        ToUserDto(task.CreatedBy),
        task.DueDate,
        task.CreatedAt,
        task.CompletedAt);

    private static TaskUserDto? ToUserDto(ApplicationUser? user) => user is null
        ? null
        : new TaskUserDto(user.Id, user.Id, user.FullName);
}
