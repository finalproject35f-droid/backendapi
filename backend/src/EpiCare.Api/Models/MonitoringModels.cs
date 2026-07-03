namespace EpiCare.Api.Models;

public enum MonitoringState
{
    Normal,
    Warning,
    Seizure,
    Offline
}

public sealed record PatientLatestState
{
    public string PatientId { get; init; } = "";
    public string DeviceId { get; init; } = "";
    public MonitoringState State { get; init; }
    public string Command { get; init; } = "NORMAL";
    public SensorReadingDto? LatestReading { get; init; }
    public AiPredictionResult? Prediction { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public bool IsConnected { get; init; } = true;
}

public sealed record PatientAlert
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PatientId { get; init; } = "";
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record SeizureEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PatientId { get; init; } = "";
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; init; }
    public string Status { get; init; } = "predicted";
    public SensorReadingDto? TriggerReading { get; init; }
    public AiPredictionResult? Prediction { get; init; }
}

public sealed record IngestReadingResponse
{
    public string PatientId { get; init; } = "";
    public MonitoringState State { get; init; }
    public string DeviceCommand { get; init; } = "NORMAL";
    public AiPredictionResult Prediction { get; init; } = new();
    public int WindowSampleCount { get; init; }
}
