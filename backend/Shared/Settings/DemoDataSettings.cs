namespace Shared.Settings;

/// <summary>
/// Controls development-only demo accounts and clinic data.
/// Demo data is disabled unless explicitly enabled through configuration.
/// </summary>
public sealed class DemoDataSettings
{
    public bool Enabled { get; init; }
    public string OwnerEmail { get; init; } = "owner@clinicbook.local";
    public string DoctorEmail { get; init; } = "doctor@clinicbook.local";
    public string PatientEmail { get; init; } = "patient@clinicbook.local";
    public string Password { get; init; } = "ClinicBook123!";
}
