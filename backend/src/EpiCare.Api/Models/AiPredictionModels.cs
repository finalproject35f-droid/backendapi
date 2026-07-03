using System.Text.Json.Serialization;

namespace EpiCare.Api.Models;

public sealed record AiPredictionRequest
{
    [JsonPropertyName("eeg")]
    public IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> Eeg { get; init; } =
        Array.Empty<IReadOnlyList<IReadOnlyList<double>>>();

    [JsonPropertyName("ecg")]
    public IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> Ecg { get; init; } =
        Array.Empty<IReadOnlyList<IReadOnlyList<double>>>();

    [JsonPropertyName("emg")]
    public IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> Emg { get; init; } =
        Array.Empty<IReadOnlyList<IReadOnlyList<double>>>();
}

public sealed record AiPredictionResult
{
    public string Label { get; init; } = "Normal";
    public double Probability { get; init; }
    public bool? Triggered { get; init; }
    public double? TriggerProbability { get; init; }
    public double? FusionProbability { get; init; }
    public int? FinalPrediction { get; init; }
    public string? Message { get; init; }
    public double? ProcessingTimeMs { get; init; }
    public string Source { get; init; } = "fallback";
    public object? Raw { get; init; }
}
