namespace EpiCare.Api.Models;

public sealed record AiModelOptions
{
    public string BaseUrl { get; init; } = "https://seizeit2-api-production.up.railway.app";
    public string PredictPath { get; init; } = "/predict";
    public int WindowSize { get; init; } = 240;
    public int ModelWindowCount { get; init; } = 8;
    public int ModelSamplesPerWindow { get; init; } = 3840;
    public int ModelEegChannels { get; init; } = 2;
    public int ModelEcgChannels { get; init; } = 1;
    public int ModelEmgChannels { get; init; } = 1;
    public double HighRiskThreshold { get; init; } = 0.7;
    public int TimeoutSeconds { get; init; } = 60;
}

public sealed record DecisionOptions
{
    public bool DemoMode { get; init; }
    public double AccGravityBaseline { get; init; } = 980;
    public double AccVerticalScale { get; init; } = 50;
    public double AccMovementThreshold { get; init; } = 2.5;
}
