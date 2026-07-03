namespace EpiCare.Api.Models;

public sealed record SensorReadingDto
{
    public string PatientId { get; init; } = "demo-patient";
    public string DeviceId { get; init; } = "proteus-simulator";
    public IReadOnlyList<double> Eeg { get; init; } = Array.Empty<double>();
    public double Ecg { get; init; }
    public double Emg { get; init; }
    public IReadOnlyList<double>? Acc { get; init; }
    public string? State { get; init; }
    public DateTimeOffset? CapturedAt { get; init; }
}
