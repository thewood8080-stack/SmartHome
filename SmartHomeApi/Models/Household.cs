using System.Security.Cryptography;

namespace SmartHomeApi.Models;

/// <summary>משק בית — יחידת הבידוד של כל הנתונים במערכת.</summary>
public class Household
{
    /// <summary>אותיות וספרות בלבד, בלי תווים שקל לבלבל ביניהם (0/O, 1/I).</summary>
    private const string InviteCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>קוד הצטרפות בן 6 תווים גדולים, ייחודי במערכת.</summary>
    public string InviteCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();

    public static string GenerateInviteCode()
    {
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = InviteCodeAlphabet[RandomNumberGenerator.GetInt32(InviteCodeAlphabet.Length)];
        return new string(chars);
    }
}
