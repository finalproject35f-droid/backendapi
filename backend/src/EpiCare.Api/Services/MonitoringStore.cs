using System.Collections.Concurrent;
using EpiCare.Api.Models;
using Microsoft.Extensions.Options;

namespace EpiCare.Api.Services;

public sealed class MonitoringStore
{
    private readonly int _windowSize;
    private readonly SignalPreprocessor _preprocessor;
    private readonly ConcurrentDictionary<string, PatientBuffer> _buffers = new();
    private readonly ConcurrentDictionary<string, PatientLatestState> _latest = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<PatientAlert>> _alerts = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<SeizureEvent>> _events = new();

    public MonitoringStore(IOptions<AiModelOptions> options, SignalPreprocessor preprocessor)
    {
        _windowSize = Math.Max(5, options.Value.WindowSize);
        _preprocessor = preprocessor;
    }

    public int AddReading(SensorReadingDto reading)
    {
        var buffer = _buffers.GetOrAdd(reading.PatientId, _ => new PatientBuffer(_windowSize));
        return buffer.Add(reading);
    }

    public AiPredictionRequest BuildWindow(string patientId)
    {
        if (!_buffers.TryGetValue(patientId, out var buffer))
        {
            return new AiPredictionRequest();
        }

        var readings = buffer.Snapshot();

        return _preprocessor.BuildModelInput(readings);
    }

    public int GetWindowSampleCount(string patientId)
    {
        return _buffers.TryGetValue(patientId, out var buffer) ? buffer.Count : 0;
    }

    public IReadOnlyList<SensorReadingDto> GetRecentReadings(string patientId, int take)
    {
        if (!_buffers.TryGetValue(patientId, out var buffer))
        {
            return Array.Empty<SensorReadingDto>();
        }

        return buffer.Snapshot().TakeLast(Math.Clamp(take, 1, 500)).ToArray();
    }

    public void SetLatest(PatientLatestState state)
    {
        _latest[state.PatientId] = state;
    }

    public PatientLatestState? GetLatest(string patientId)
    {
        return _latest.TryGetValue(patientId, out var state) ? state : null;
    }

    public void AddAlert(PatientAlert alert)
    {
        var alerts = _alerts.GetOrAdd(alert.PatientId, _ => new ConcurrentQueue<PatientAlert>());
        alerts.Enqueue(alert);
        Trim(alerts, 100);
    }

    public IReadOnlyList<PatientAlert> GetAlerts(string patientId)
    {
        return _alerts.TryGetValue(patientId, out var alerts)
            ? alerts.Reverse().ToArray()
            : Array.Empty<PatientAlert>();
    }

    public void AddEvent(SeizureEvent seizureEvent)
    {
        var events = _events.GetOrAdd(seizureEvent.PatientId, _ => new ConcurrentQueue<SeizureEvent>());
        events.Enqueue(seizureEvent);
        Trim(events, 100);
    }

    public IReadOnlyList<SeizureEvent> GetEvents(string patientId)
    {
        return _events.TryGetValue(patientId, out var events)
            ? events.Reverse().ToArray()
            : Array.Empty<SeizureEvent>();
    }

    private static void Trim<T>(ConcurrentQueue<T> queue, int maxItems)
    {
        while (queue.Count > maxItems && queue.TryDequeue(out _))
        {
        }
    }

    private sealed class PatientBuffer
    {
        private readonly int _capacity;
        private readonly Queue<SensorReadingDto> _readings = new();
        private readonly object _sync = new();

        public PatientBuffer(int capacity)
        {
            _capacity = capacity;
        }

        public int Count
        {
            get
            {
                lock (_sync)
                {
                    return _readings.Count;
                }
            }
        }

        public int Add(SensorReadingDto reading)
        {
            lock (_sync)
            {
                _readings.Enqueue(reading);
                while (_readings.Count > _capacity)
                {
                    _readings.Dequeue();
                }
                return _readings.Count;
            }
        }

        public IReadOnlyList<SensorReadingDto> Snapshot()
        {
            lock (_sync)
            {
                return _readings.ToArray();
            }
        }
    }
}
