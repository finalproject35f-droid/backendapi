using EpiCare.Api.Models;
using Microsoft.Extensions.Options;

namespace EpiCare.Api.Services;

public sealed class SignalPreprocessor
{
    private readonly AiModelOptions _options;

    public SignalPreprocessor(IOptions<AiModelOptions> options)
    {
        _options = options.Value;
    }

    public AiPredictionRequest BuildModelInput(IReadOnlyList<SensorReadingDto> readings)
    {
        if (readings.Count == 0)
        {
            return new AiPredictionRequest();
        }

        return new AiPredictionRequest
        {
            Eeg = BuildTensor(
                readings,
                _options.ModelWindowCount,
                _options.ModelEegChannels,
                _options.ModelSamplesPerWindow,
                (reading, channel) => channel < reading.Eeg.Count ? reading.Eeg[channel] : reading.Eeg.FirstOrDefault()),
            Ecg = BuildTensor(
                readings,
                _options.ModelWindowCount,
                _options.ModelEcgChannels,
                _options.ModelSamplesPerWindow,
                (reading, _) => reading.Ecg),
            Emg = BuildTensor(
                readings,
                _options.ModelWindowCount,
                _options.ModelEmgChannels,
                _options.ModelSamplesPerWindow,
                (reading, _) => reading.Emg)
        };
    }

    private static IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> BuildTensor(
        IReadOnlyList<SensorReadingDto> readings,
        int windowCount,
        int channelCount,
        int samplesPerWindow,
        Func<SensorReadingDto, int, double> readValue)
    {
        var totalSamples = Math.Max(1, windowCount * samplesPerWindow);
        var tensor = new double[windowCount][][];

        for (var window = 0; window < windowCount; window++)
        {
            tensor[window] = new double[channelCount][];

            for (var channel = 0; channel < channelCount; channel++)
            {
                var samples = new double[samplesPerWindow];

                for (var sample = 0; sample < samplesPerWindow; sample++)
                {
                    var globalSample = window * samplesPerWindow + sample;
                    var readingIndex = ScaleIndex(globalSample, totalSamples, readings.Count);
                    samples[sample] = readValue(readings[readingIndex], channel);
                }

                tensor[window][channel] = ZScore(samples);
            }
        }

        return tensor
            .Select(window => window
                .Select(channel => (IReadOnlyList<double>)channel)
                .ToArray())
            .Cast<IReadOnlyList<IReadOnlyList<double>>>()
            .ToArray();
    }

    private static int ScaleIndex(int globalSample, int totalSamples, int readingCount)
    {
        if (readingCount <= 1)
        {
            return 0;
        }

        var ratio = globalSample / (double)Math.Max(1, totalSamples - 1);
        return Math.Clamp((int)Math.Round(ratio * (readingCount - 1)), 0, readingCount - 1);
    }

    private static double[] ZScore(double[] values)
    {
        var mean = values.Average();
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Length;
        var std = Math.Sqrt(variance);

        if (std < 1e-6)
        {
            return values.Select(_ => 0d).ToArray();
        }

        return values.Select(value => (value - mean) / std).ToArray();
    }
}
