using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace SmartHomeApi.Services;

/// <summary>
/// מונה המבקרים ורשימת המחוברים נשמרים ב-IMemoryCache.
/// "מחובר" = ביצע בקשה בתוך חלון הפעילות; כך המספר לא נתקע כלפי מעלה
/// כשמשתמש סוגר את הדפדפן בלי לצאת.
/// </summary>
public class AppStatsService : IAppStatsService
{
    private const string VisitorsKey = "stats:visitors";
    private const string ActiveUsersKey = "stats:active-users";

    /// <summary>אחרי חוסר פעילות בפרק הזמן הזה המשתמש נחשב מנותק.</summary>
    private static readonly TimeSpan ActivityWindow = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    /// <summary>נעילה על המונה בלבד — Interlocked לא עובד על ערך שיושב בתוך ה-cache.</summary>
    private readonly object _visitorsLock = new();

    public AppStatsService(IMemoryCache cache)
    {
        _cache = cache;
        SinceUtc = DateTime.UtcNow;
    }

    public DateTime SinceUtc { get; }

    public long TotalVisitors => _cache.TryGetValue(VisitorsKey, out long count) ? count : 0;

    public int ConnectedUsers
    {
        get
        {
            var cutoff = DateTime.UtcNow - ActivityWindow;
            return ActiveUsers.Count(entry => entry.Value >= cutoff);
        }
    }

    /// <summary>הערכים לעולם לא פגים — ה-cache משמש כאן כאחסון בזיכרון, לא כמטמון.</summary>
    private ConcurrentDictionary<string, DateTime> ActiveUsers =>
        _cache.GetOrCreate(ActiveUsersKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return new ConcurrentDictionary<string, DateTime>();
        })!;

    public void TrackVisit()
    {
        lock (_visitorsLock)
        {
            var current = TotalVisitors;
            _cache.Set(VisitorsKey, current + 1, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            });
        }
    }

    public void TrackActiveUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        ActiveUsers[userId] = DateTime.UtcNow;
        PurgeStale();
    }

    public void RemoveActiveUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        ActiveUsers.TryRemove(userId, out _);
    }

    /// <summary>מנקה משתמשים שיצאו מחלון הפעילות, כדי שהמילון לא יתפח לאורך זמן.</summary>
    private void PurgeStale()
    {
        var cutoff = DateTime.UtcNow - ActivityWindow;
        foreach (var entry in ActiveUsers.Where(e => e.Value < cutoff))
            ActiveUsers.TryRemove(entry.Key, out _);
    }
}
