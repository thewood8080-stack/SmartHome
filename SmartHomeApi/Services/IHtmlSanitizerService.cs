namespace SmartHomeApi.Services;

/// <summary>
/// מנקה HTML שמגיע מעורך הטקסט העשיר בצד הלקוח.
/// חובה להריץ על כל טקסט עשיר לפני שמירה — אחרת זו פרצת XSS.
/// </summary>
public interface IHtmlSanitizerService
{
    /// <summary>מחזיר HTML נקי מתגיות ומאפיינים מסוכנים. null הופך למחרוזת ריקה.</summary>
    string Sanitize(string? html);
}
