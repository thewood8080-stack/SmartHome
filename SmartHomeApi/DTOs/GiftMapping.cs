using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// תרגום מתנה ל-DTO. מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// </summary>
public static class GiftMapping
{
    /// <summary>
    /// המפריד בין רעיונות בתוך העמודה היחידה שבה הם נשמרים.
    /// נבחר תו שלא מופיע בטקסט חופשי בעברית, ובפרט לא פסיק —
    /// ה-client מפריד את הקלט בפסיקים, ופסיק כמפריד באחסון היה שובר רעיון שמכיל פסיק.
    /// </summary>
    private const string IdeasSeparator = " | ";

    public static GiftDto ToDto(Gift gift) => new(
        gift.Id.ToString(),
        gift.Id.ToString(),
        gift.RecipientName,
        gift.Occasion,
        gift.Date,
        SplitIdeas(gift.Ideas),
        gift.IsPurchased,
        gift.PurchasedItem,
        gift.Note,
        gift.EventId?.ToString(),
        ToEventDto(gift.Event),
        gift.CreatedAt);

    private static GiftEventDto? ToEventDto(Event? ev) => ev is null
        ? null
        : new GiftEventDto(ev.Id.ToString(), ev.Title, ev.StartDate);

    /// <summary>המערך שמגיע מה-client נשמר כמחרוזת אחת.</summary>
    public static string JoinIdeas(IEnumerable<string>? ideas) => ideas is null
        ? string.Empty
        : string.Join(IdeasSeparator, ideas
            .Select(i => i.Trim())
            .Where(i => i.Length > 0));

    /// <summary>ובחזרה למערך. רשומות ישנות עם מחרוזת ריקה מחזירות מערך ריק, לא [""].</summary>
    private static IReadOnlyList<string> SplitIdeas(string? ideas) =>
        string.IsNullOrWhiteSpace(ideas)
            ? []
            : ideas.Split(IdeasSeparator, StringSplitOptions.None)
                .Select(i => i.Trim())
                .Where(i => i.Length > 0)
                .ToList();

    /// <summary>ה-client שולח מחרוזת ריקה כשהשדה לא מולא.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
