using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// תרגום אירוע ל-DTO. מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// </summary>
public static class EventMapping
{
    /// <summary>ברירת המחדל של Color במודל, לשימוש כשה-client שולח שדה ריק.</summary>
    public const string DefaultColor = "#1E3A5F";

    public static EventDto ToDto(Event ev) => new(
        ev.Id.ToString(),
        ev.Id.ToString(),
        ev.Title,
        ev.Description,
        ev.StartDate,
        ev.AllDay,
        ev.Color,
        ToUserDto(ev.CreatedBy),
        ev.CreatedAt);

    private static EventUserDto? ToUserDto(ApplicationUser? user) => user is null
        ? null
        : new EventUserDto(user.FullName);

    /// <summary>ה-client שולח מחרוזת ריקה כשהשדה לא מולא.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
