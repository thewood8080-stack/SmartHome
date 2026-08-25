using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// תרגום רכב ל-DTO. מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// כאן נעשה גישור השמות בין הישות ל-client: LicensePlate→plateNumber,
/// InsuranceExpiry→insurance, TestExpiry→test.
/// </summary>
public static class VehicleMapping
{
    public static VehicleDto ToDto(Vehicle vehicle) => new(
        vehicle.Id.ToString(),
        vehicle.Id.ToString(),
        vehicle.Name,
        vehicle.LicensePlate,
        vehicle.Model,
        vehicle.Year,
        vehicle.LastService,
        vehicle.NextService,
        vehicle.InsuranceExpiry,
        vehicle.TestExpiry,
        vehicle.FuelType,
        vehicle.Notes,
        ToUserDto(vehicle.AddedBy),
        vehicle.CreatedAt);

    private static VehicleUserDto? ToUserDto(ApplicationUser? user) => user is null
        ? null
        : new VehicleUserDto(user.FullName);

    /// <summary>ה-client שולח מחרוזת ריקה כשהשדה לא מולא.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// לוחית רישוי. בניגוד לשאר השדות האופציונליים היא לא מנורמלת ל-null,
    /// כי העמודה מוגדרת NOT NULL (ApplicationDbContext.ConfigureVehicles).
    /// ריק נשמר כמחרוזת ריקה, ו-VehiclePage.tsx:125 ממילא לא מציג אותה.
    /// </summary>
    public static string NormalizePlate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
