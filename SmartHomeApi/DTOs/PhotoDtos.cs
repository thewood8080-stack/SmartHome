using System.ComponentModel.DataAnnotations;
using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>אלבום כפי שהוא נשלח ללקוח. Visibility יוצא כמחרוזת: "Family" | "Personal".</summary>
public record PhotoAlbumDto(
    int Id,
    string Name,
    string Description,
    string? CoverPhotoUrl,
    string Visibility,
    string OwnerId,
    string OwnerName,
    int PhotoCount,
    DateTime CreatedAt,
    bool CanEdit)
{
    public static PhotoAlbumDto From(PhotoAlbum album, string ownerName, int photoCount, bool canEdit) => new(
        album.Id,
        album.Name,
        album.Description,
        album.CoverPhotoUrl,
        album.Visibility.ToString(),
        album.OwnerId,
        ownerName,
        photoCount,
        album.CreatedAt,
        canEdit);
}

/// <summary>תמונה בודדת כפי שהיא נשלחת ללקוח.</summary>
public record PhotoDto(
    int Id,
    int AlbumId,
    string AlbumName,
    string Url,
    string? Caption,
    int? Width,
    int? Height,
    string UploadedById,
    string UploadedByName,
    DateTime UploadedAt)
{
    /// <summary>דורש ש-PhotoAlbum ו-UploadedBy ייטענו מראש (Include).</summary>
    public static PhotoDto From(Photo photo) => new(
        photo.Id,
        photo.PhotoAlbumId,
        photo.PhotoAlbum?.Name ?? string.Empty,
        photo.CloudinaryUrl,
        photo.Caption,
        photo.Width,
        photo.Height,
        photo.UploadedById,
        photo.UploadedBy?.FullName ?? string.Empty,
        photo.UploadedAt);
}

/// <summary>
/// תוצאת העלאה בודדת ל-Cloudinary.
/// Width ו-Height ריקים בהעלאת raw (PDF) — שם אין ממדי תמונה.
/// </summary>
public record PhotoUploadResultDto(
    string Url,
    string PublicId,
    int? Width,
    int? Height);

public class PhotoAlbumCreateDto
{
    [Required(ErrorMessage = "נדרש שם לאלבום")]
    [MaxLength(150, ErrorMessage = "שם האלבום ארוך מדי")]
    public string Name { get; set; } = string.Empty;

    /// <summary>HTML מעורך הטקסט העשיר — מנוקה בשרת לפני שמירה.</summary>
    public string? Description { get; set; }

    /// <summary>"Family" | "Personal". ריק = Family.</summary>
    public string? Visibility { get; set; }
}

/// <summary>עדכון אלבום. אותם שדות כמו ביצירה.</summary>
public class PhotoAlbumUpdateDto : PhotoAlbumCreateDto;
