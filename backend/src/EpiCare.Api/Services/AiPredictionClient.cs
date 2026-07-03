using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using EpiCare.Api.Models;
using Microsoft.Extensions.Options;

namespace EpiCare.Api.Services;

public sealed class AiPredictionClient
{
    private readonly HttpClient _httpClient;
    private readonly AiModelOptions _options;
    private readonly ILogger<AiPredictionClient> _logger;

    public AiPredictionClient(
        HttpClient httpClient,
        IOptions<AiModelOptions> options,
        ILogger<AiPredictionClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiPredictionResult> PredictAsync(
        AiPredictionRequest request,
        SensorReadingDto latestReading,
        CancellationToken cancellationToken)
    {
        if (request.Eeg.Count == 0 || request.Ecg.Count == 0 || request.Emg.Count == 0)
        {
            return BuildFallback("fallback-empty-window");
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var response = await _httpClient.PostAsJsonAsync(
                _options.PredictPath,
                request,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI model returned {StatusCode}: {Body}", response.StatusCode, body);
                return BuildFallback("fallback-ai-http-error");
            }

            var raw = JsonNode.Parse(body);
            return ParsePrediction(raw, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "AI model request failed. Returning a neutral unavailable result.");
            return BuildFallback("fallback-ai-exception");
        }
    }

    private AiPredictionResult ParsePrediction(JsonNode? raw, double elapsedMs)
    {
        if (raw is not JsonObject obj)
        {
            return new AiPredictionResult
            {
                Label = "Normal",
                Probability = 0,
                ProcessingTimeMs = elapsedMs,
                Source = "ai",
                Raw = raw
            };
        }

        var finalPrediction = ReadInt(obj, "final_prediction", "finalPrediction", "prediction");
        var triggered = ReadBool(obj, "triggered");
        var triggerProbability = ReadDouble(obj, "trigger_probability", "triggerProbability");
        var fusionProbability = ReadDouble(obj, "fusion_probability", "fusionProbability");
        var label = ReadString(obj, "label", "prediction", "class", "state", "status");
        var probability = fusionProbability
            ?? triggerProbability
            ?? ReadDouble(obj, "probability", "confidence", "risk_score", "score")
            ?? 0;
        var processingTime = ReadDouble(obj, "processing_time_ms", "processingTimeMs", "processing_time", "latency_ms") ?? elapsedMs;
        var message = ReadString(obj, "message");

        return new AiPredictionResult
        {
            Label = NormalizeLabel(label, finalPrediction),
            Probability = probability,
            Triggered = triggered,
            TriggerProbability = triggerProbability,
            FusionProbability = fusionProbability,
            FinalPrediction = finalPrediction,
            Message = message,
            ProcessingTimeMs = processingTime,
            Source = "ai",
            Raw = raw
        };
    }

    private static string? ReadString(JsonObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj.TryGetPropertyValue(key, out var value) && value is not null)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static double? ReadDouble(JsonObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!obj.TryGetPropertyValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is JsonValue jsonValue &&
                jsonValue.TryGetValue<double>(out var number))
            {
                return number;
            }

            if (double.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static int? ReadInt(JsonObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!obj.TryGetPropertyValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is JsonValue jsonValue &&
                jsonValue.TryGetValue<int>(out var number))
            {
                return number;
            }

            if (int.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool? ReadBool(JsonObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!obj.TryGetPropertyValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is JsonValue jsonValue &&
                jsonValue.TryGetValue<bool>(out var boolean))
            {
                return boolean;
            }

            if (bool.TryParse(value.ToString(), out var parsedBool))
            {
                return parsedBool;
            }

            if (int.TryParse(value.ToString(), out var parsedInt))
            {
                return parsedInt != 0;
            }
        }

        return null;
    }

    private static string NormalizeLabel(string? label, int? finalPrediction)
    {
        if (finalPrediction is not null)
        {
            return finalPrediction == 1 ? "Warning" : "Normal";
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return "Normal";
        }

        var normalized = label.Trim().ToLowerInvariant();
        return normalized switch
        {
            "p" or "prediction" or "warning" or "preictal" => "Warning",
            "s" or "seizure" or "ictal" => "Seizure",
            "n" or "normal" or "safe" => "Normal",
            _ => label
        };
    }

    private static AiPredictionResult BuildFallback(string source)
    {
        return new AiPredictionResult
        {
            Label = "Normal",
            Probability = 0,
            Source = source,
            Raw = new { reason = "ai_unavailable" }
        };
    }
}
