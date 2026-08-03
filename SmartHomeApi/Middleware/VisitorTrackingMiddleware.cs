using System.Security.Claims;
using SmartHomeApi.Services;

namespace SmartHomeApi.Middleware;

/// <summary>
/// מזין את מוני ה-Application state: מבקר חדש נספר פעם אחת לכל סשן,
/// ומשתמש מזוהה מסומן כפעיל בכל בקשה.
/// חייב לרוץ אחרי UseSession ו-UseAuthentication.
/// </summary>
public class VisitorTrackingMiddleware
{
    private const string CountedKey = "Counted";

    private readonly RequestDelegate _next;

    public VisitorTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAppStatsService stats)
    {
        // הבקשה הראשונה בסשן היא "מבקר חדש"; הבאות אחריה כבר לא.
        if (context.Session.GetString(CountedKey) is null)
        {
            context.Session.SetString(CountedKey, "1");
            stats.TrackVisit();
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
                stats.TrackActiveUser(userId);
        }

        await _next(context);
    }
}
