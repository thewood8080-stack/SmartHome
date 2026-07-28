using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Models;

namespace SmartHomeApi.Data;

/// <summary>
/// זריעה ראשונית: תפקידים, משק בית לדוגמה ומשתמש מנהל.
/// הסיסמה נלקחת מקונפיגורציה שלא נכנסת ל-Git (appsettings.Local.json / משתנה סביבה).
/// </summary>
public static class DbSeeder
{
    public const string ManagerRole = "Manager";
    public const string MemberRole = "Member";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

        await db.Database.MigrateAsync();

        foreach (var role in new[] { ManagerRole, MemberRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var householdName = config["Seed:HouseholdName"] ?? "משפחה לדוגמה";
        var household = await db.Households.FirstOrDefaultAsync(h => h.Name == householdName);
        if (household is null)
        {
            household = new Household
            {
                Name = householdName,
                InviteCode = await GenerateUniqueInviteCodeAsync(db)
            };
            db.Households.Add(household);
            await db.SaveChangesAsync();
            logger.LogInformation("נוצר משק בית '{Name}' עם קוד הצטרפות {Code}", household.Name, household.InviteCode);
        }

        var email = config["Seed:ManagerEmail"] ?? "admin@smarthome.local";
        var password = config["Seed:ManagerPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "לא הוגדרה Seed:ManagerPassword — משתמש המנהל לא נוצר. " +
                "העתק את appsettings.Local.example.json ל-appsettings.Local.json והגדר סיסמה.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var manager = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = config["Seed:ManagerFullName"] ?? "מנהל המערכת",
            IsApproved = true,
            IsManager = true,
            HouseholdId = household.Id
        };

        var result = await userManager.CreateAsync(manager, password);
        if (!result.Succeeded)
        {
            logger.LogError("יצירת משתמש המנהל נכשלה: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(manager, ManagerRole);
        logger.LogInformation("נוצר משתמש מנהל: {Email}", email);
    }

    private static async Task<string> GenerateUniqueInviteCodeAsync(ApplicationDbContext db)
    {
        while (true)
        {
            var code = Household.GenerateInviteCode();
            if (!await db.Households.AnyAsync(h => h.InviteCode == code))
                return code;
        }
    }
}
