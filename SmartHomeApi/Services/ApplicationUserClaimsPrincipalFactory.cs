using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartHomeApi.Models;

namespace SmartHomeApi.Services;

/// <summary>
/// מוסיף לעוגיית ההזדהות את משק הבית, השם ומצב האישור.
/// זהו גיבוי ל-Session: אם הסשן פג לפני העוגייה, הבקשה עדיין יודעת לאיזה בית לסנן
/// בלי לגשת לבסיס הנתונים.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public const string ApprovedClaimType = "approved";

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(CurrentUserService.FullNameClaimType, user.FullName));
        identity.AddClaim(new Claim(
            ApprovedClaimType, user.IsApproved.ToString(), ClaimValueTypes.Boolean));

        if (user.HouseholdId.HasValue)
        {
            identity.AddClaim(new Claim(
                CurrentUserService.HouseholdClaimType,
                user.HouseholdId.Value.ToString()));
        }

        return identity;
    }
}
