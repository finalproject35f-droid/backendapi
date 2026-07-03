using EpiCare.Api.Models;
using Microsoft.Extensions.Options;

namespace EpiCare.Api.Services;

public sealed class DecisionService
{
    private readonly AiModelOptions _options;
    private readonly DecisionOptions _decisionOptions;

    public DecisionService(
        IOptions<AiModelOptions> options,
        IOptions<DecisionOptions> decisionOptions)
    {
        _options = options.Value;
        _decisionOptions = decisionOptions.Value;
    }

    public MonitoringState Decide(AiPredictionResult prediction, SensorReadingDto? latestReading = null)
    {
        if (_decisionOptions.DemoMode)
        {
            var simulatorState = latestReading?.State?.Trim().ToUpperInvariant();
            if (simulatorState is "S" or "SEIZURE")
            {
                return MonitoringState.Seizure;
            }

            if (simulatorState is "P" or "WARNING" or "PREDICTION")
            {
                return MonitoringState.Warning;
            }
        }

        var label = prediction.Label.Trim().ToLowerInvariant();
        var modelDetectedSeizure = label is "seizure" or "ictal";
        var modelDetectedHighRisk = prediction.FinalPrediction == 1
            || label is "warning" or "prediction" or "preictal"
            || prediction.Probability >= _options.HighRiskThreshold;

        if (modelDetectedSeizure ||
            modelDetectedHighRisk && HasAbnormalMovement(latestReading?.Acc))
        {
            return MonitoringState.Seizure;
        }

        if (modelDetectedHighRisk)
        {
            return MonitoringState.Warning;
        }

        return MonitoringState.Normal;
    }

    private bool HasAbnormalMovement(IReadOnlyList<double>? acc)
    {
        if (acc is not { Count: >= 3 })
        {
            return false;
        }

        var horizontalMovement = Math.Sqrt((acc[0] * acc[0]) + (acc[1] * acc[1]));
        var verticalMovement = Math.Abs(acc[2] - _decisionOptions.AccGravityBaseline)
            / Math.Max(1, _decisionOptions.AccVerticalScale);

        return horizontalMovement + verticalMovement >= _decisionOptions.AccMovementThreshold;
    }

    public string ToDeviceCommand(MonitoringState state)
    {
        return state switch
        {
            MonitoringState.Warning => "P",
            MonitoringState.Seizure => "S",
            _ => "N"
        };
    }

    public string ToApplicationState(MonitoringState state)
    {
        return state switch
        {
            MonitoringState.Warning => "HIGH_RISK",
            MonitoringState.Seizure => "SEIZURE_DETECTED",
            MonitoringState.Offline => "OFFLINE",
            _ => "NORMAL"
        };
    }

    public double ToApplicationProbability(MonitoringState state, AiPredictionResult? prediction)
    {
        var probability = prediction?.FusionProbability
            ?? prediction?.TriggerProbability
            ?? prediction?.Probability
            ?? 0;

        return state switch
        {
            MonitoringState.Seizure => Math.Max(probability, 0.85),
            MonitoringState.Warning => Math.Max(probability, _options.HighRiskThreshold),
            MonitoringState.Offline => 0,
            _ => probability
        };
    }
}
