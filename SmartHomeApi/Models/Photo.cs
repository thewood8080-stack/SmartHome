namespace SmartHomeApi.Models;

/// <summary>תמונה בודדת באלבום — הקובץ עצמו מאוחסן ב-Cloudinary.</summary>
public class Photo : IHouseholdOwned
{
    public int Id { get; set; }

    public int PhotoAlbumId { get; set; }
    public PhotoAlbum? PhotoAlbum { get; set; }

    public string CloudinaryUrl { get; set; } = string.Empty;

    /// <summary>מזהה הקובץ ב-Cloudinary — נדרש כדי למחוק אותו משם.</summary>
    public string CloudinaryPublicId { get; set; } = string.Empty;

    public string? Caption { get; set; }

    public string UploadedById { get; set; } = string.Empty;
    public ApplicationUser? UploadedBy { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
