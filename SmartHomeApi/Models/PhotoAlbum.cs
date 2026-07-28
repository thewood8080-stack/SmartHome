namespace SmartHomeApi.Models;

/// <summary>אלבום תמונות.</summary>
public class PhotoAlbum : IHouseholdOwned
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>HTML מעורך הטקסט העשיר.</summary>
    public string Description { get; set; } = string.Empty;

    public string? CoverPhotoUrl { get; set; }

    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }

    public AlbumVisibility Visibility { get; set; } = AlbumVisibility.Family;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
