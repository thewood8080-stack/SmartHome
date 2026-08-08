using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// פעולות על תמונה בודדת.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על Photo.
/// </summary>
[ApiController]
[Route("api/photos")]
[Authorize]
[Produces("application/json")]
public class PhotosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICloudinaryService _cloudinary;

    public PhotosController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        ICloudinaryService cloudinary)
    {
        _db = db;
        _currentUser = currentUser;
        _cloudinary = cloudinary;
    }

    /// <summary>
    /// מחיקת תמונה משני המקומות.
    /// Cloudinary קודם: אם המחיקה שם נכשלה, הרשומה נשארת בבסיס הנתונים
    /// ומוחזרת שגיאה — כדי שלא ייווצר קובץ יתום באחסון.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Delete(int id)
    {
        var photo = await _db.Photos
            .Include(p => p.PhotoAlbum)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (photo?.PhotoAlbum is null || !CanView(photo.PhotoAlbum))
            return NotFound(new MessageResponse("התמונה לא נמצאה"));

        if (!CanDelete(photo))
            return Forbid();

        if (!await _cloudinary.DeleteAsync(photo.CloudinaryPublicId, isRaw: false))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new MessageResponse(
                "מחיקת התמונה מהאחסון נכשלה. התמונה לא נמחקה — נסה שוב."));
        }

        var album = photo.PhotoAlbum;

        _db.Photos.Remove(photo);

        // אם זו הייתה תמונת השער, מחליפים בתמונה אחרת שנשארה באלבום.
        if (album.CoverPhotoUrl == photo.CloudinaryUrl)
        {
            album.CoverPhotoUrl = await _db.Photos
                .Where(p => p.PhotoAlbumId == album.Id && p.Id != photo.Id)
                .OrderByDescending(p => p.UploadedAt)
                .Select(p => p.CloudinaryUrl)
                .FirstOrDefaultAsync();
        }

        await _db.SaveChangesAsync();

        return Ok(new MessageResponse("התמונה נמחקה"));
    }

    private bool CanView(PhotoAlbum album) =>
        album.Visibility == AlbumVisibility.Family
        || album.OwnerId == _currentUser.UserId
        || _currentUser.IsManager;

    /// <summary>מנהל מוחק כל תמונה. משתמש רגיל — רק תמונה שהוא עצמו העלה.</summary>
    private bool CanDelete(Photo photo) =>
        _currentUser.IsManager || photo.UploadedById == _currentUser.UserId;
}
