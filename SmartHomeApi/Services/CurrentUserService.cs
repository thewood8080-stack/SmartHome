using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using SmartHomeApi.Data;

namespace SmartHomeApi.Services;

/// <summary>
/// קורא את משק הבית ואת שם המשתמש מה-Session, ואם אין — מה-Claims של עוגיית ההזדהות.
/// שתי הדרכים נשמרות ב-Login, כך שגם אם ה-Session פג העוגייה עדיין מחזיקה את השיוך.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public const string HouseholdSessionKey = "HouseholdId";
    public const string FullNameSessionKey = "FullName";

    public const string HouseholdClaimType = "household_id";
    public const string FullNameClaimType = "full_name";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public int? HouseholdId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return null;

            if (HasSession(context))
            {
                var fromSession = context.Session.GetInt32(HouseholdSessionKey);
                if (fromSession.HasValue)
                    return fromSession;
            }

            var fromClaim = context.User?.FindFirstValue(HouseholdClaimType);
            return int.TryParse(fromClaim, out var householdId) ? householdId : null;
        }
    }

    public string? FullName
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return null;

            if (HasSession(context))
            {
                var fromSession = context.Session.GetString(FullNameSessionKey);
                if (!string.IsNullOrWhiteSpace(fromSession))
                    return fromSession;
            }

            return context.User?.FindFirstValue(FullNameClaimType);
        }
    }

    public bool IsManager =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(DbSeeder.ManagerRole) ?? false;

    public void SetHousehold(int? householdId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null || !HasSession(context))
            return;

        if (householdId.HasValue)
            context.Session.SetInt32(HouseholdSessionKey, householdId.Value);
        else
            context.Session.Remove(HouseholdSessionKey);
    }

    public void SetFullName(string? fullName)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null || !HasSession(context))
            return;

        if (!string.IsNullOrWhiteSpace(fullName))
            context.Session.SetString(FullNameSessionKey, fullName);
        else
            context.Session.Remove(FullNameSessionKey);
    }

    public void Clear()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null || !HasSession(context))
            return;

        context.Session.Clear();
    }

    /// <summary>
    /// ctx.Session זורק אם ה-Session middleware לא רץ עבור הבקשה הזו (למשל ב-seed בהפעלה).
    /// </summary>
    private static bool HasSession(HttpContext context) =>
        context.Features.Get<ISessionFeature>() is not null;
}
