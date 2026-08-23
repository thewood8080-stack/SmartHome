using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// כל התרגום בין ה-enum של המודל למחרוזות שה-client מכיר.
/// מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// שים לב: ערך ה-enum נקרא Other, אבל ה-client מכיר אותו כ-'note'.
/// </summary>
public static class MedicalMapping
{
    private static readonly Dictionary<MedicalRecordType, string> TypeToClient = new()
    {
        [MedicalRecordType.Appointment] = "appointment",
        [MedicalRecordType.Medication] = "medication",
        [MedicalRecordType.Vaccine] = "vaccine",
        [MedicalRecordType.Other] = "note"
    };

    private static readonly Dictionary<string, MedicalRecordType> ClientToType =
        TypeToClient.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static string ToClientType(MedicalRecordType type) => TypeToClient[type];

    /// <summary>מתרגם 'appointment'/'medication'/'vaccine'/'note'. מחרוזת ריקה נופלת לברירת המחדל.</summary>
    public static bool TryParseType(string? value, out MedicalRecordType type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            type = MedicalRecordType.Appointment;
            return true;
        }

        return ClientToType.TryGetValue(value.Trim(), out type);
    }

    public static MedicalRecordDto ToDto(MedicalRecord record) => new(
        record.Id.ToString(),
        record.Id.ToString(),
        ToMemberDto(record.Member),
        ToClientType(record.Type),
        record.Title,
        record.Date,
        record.Doctor,
        record.Clinic,
        record.Notes,
        record.NextAppointment,
        record.CreatedAt);

    private static MedicalMemberDto? ToMemberDto(ApplicationUser? user) => user is null
        ? null
        : new MedicalMemberDto(user.Id, user.FullName);

    /// <summary>ה-client שולח מחרוזת ריקה כשהשדה לא מולא.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
