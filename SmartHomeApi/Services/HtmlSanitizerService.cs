using Ganss.Xss;

namespace SmartHomeApi.Services;

/// <summary>
/// עטיפה ל-HtmlSanitizer עם רשימת היתר מצומצמת — רק עיצוב טקסט בסיסי.
/// גישת allow-list: כל מה שלא הותר במפורש מוסר, כולל script, iframe,
/// מאפייני on* ו-href מסוג javascript:.
/// </summary>
public class HtmlSanitizerService : IHtmlSanitizerService
{
    /// <summary>
    /// HtmlSanitizer בטוח לשימוש חוזר בין בקשות (thread-safe לקריאה),
    /// ולכן מוגדר פעם אחת כ-static ולא נבנה מחדש בכל בקשה.
    /// </summary>
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return Sanitizer.Sanitize(html);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // מתחילים מרשימה ריקה כדי לא לרשת ברירות מחדל רחבות מדי.
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedCssProperties.Clear();

        // עיצוב טקסט בלבד — מה ש-TinyMCE מייצר בפועל בתיאור משימה.
        foreach (var tag in new[]
                 {
                     "p", "br", "b", "strong", "i", "em", "u", "s",
                     "ul", "ol", "li", "blockquote", "a", "span",
                     "h1", "h2", "h3", "h4"
                 })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        // קישורים מותרים, אבל רק לסכימות בטוחות — ראה AllowedSchemes למטה.
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("title");

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");

        return sanitizer;
    }
}
