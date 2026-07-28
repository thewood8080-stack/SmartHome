using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;

namespace SmartHomeApi.Services;

/// <summary>
/// קורא את משק הבית מה-Session, ואם אין — מה-Claims של עוגיית ההזדהות.
/// שתי הדרכים נשמרות ב-Login, כך שגם אם ה-Session פג העוגייה עדיין מחזיקה את השיוך.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public const string HouseholdSessionKey = "HouseholdId";
    public const string HouseholdClaimType = "household_id";

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

            // ctx.Session זורק אם ה-Session middleware לא רץ עבור הבקשה הזו (למשל ב-seed בהפעלה).
            if (context.Features.Get<ISessionFeature>() is not null)
            {
                var fromSession = context.Session.GetInt32(HouseholdSessionKey);
                if (fromSession.HasValue)
                    return fromSession;
            }

            var fromClaim = context.User?.FindFirstValue(HouseholdClaimType);
            return int.TryParse(fromClaim, out var householdId) ? householdId : null;
        }
    }

    public void SetHousehold(int? householdId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null || context.Features.Get<ISessionFeature>() is null)
            return;

        if (householdId.HasValue)
            context.Session.SetInt32(HouseholdSessionKey, householdId.Value);
        else
            context.Session.Remove(HouseholdSessionKey);
    }
}
