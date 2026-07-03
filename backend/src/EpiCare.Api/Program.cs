using EpiCare.Api.Hubs;
using EpiCare.Api.Models;
using EpiCare.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AiModelOptions>(builder.Configuration.GetSection("AiModel"));
builder.Services.Configure<DecisionOptions>(builder.Configuration.GetSection("Decision"));
builder.Services.AddSingleton<SignalPreprocessor>();
builder.Services.AddSingleton<MonitoringStore>();
builder.Services.AddSingleton<DecisionService>();
builder.Services.AddHttpClient<AiPredictionClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiModelOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(item => item.Value)
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .Cast<string>()
    .ToArray();

if (allowedOrigins.Length == 0)
{
    allowedOrigins = ["*"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("FlutterCors", policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("FlutterCors");

app.MapGet("/", () => Results.Ok(new
{
    message = "EpiCare API is running",
    docs = "/swagger",
    health = "/api/health"
}));

app.MapGet("/api/health", (IOptions<AiModelOptions> aiOptions) => Results.Ok(new
{
    status = "ok",
    service = "EpiCare Backend",
    aiModel = aiOptions.Value.BaseUrl,
    aiPredictPath = aiOptions.Value.PredictPath,
    timestamp = DateTimeOffset.UtcNow
}));

app.MapPost("/api/iot/readings", async (
    SensorReadingDto request,
    MonitoringStore store,
    AiPredictionClient aiClient,
    DecisionService decisionService,
    IHubContext<PatientMonitoringHub> hub,
    CancellationToken cancellationToken) =>
{
    var reading = request with
    {
        PatientId = string.IsNullOrWhiteSpace(request.PatientId) ? "demo-patient" : request.PatientId,
        DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? "proteus-simulator" : request.DeviceId,
        CapturedAt = request.CapturedAt ?? DateTimeOffset.UtcNow
    };

    var previousState = store.GetLatest(reading.PatientId)?.State;
    store.AddReading(reading);

    var window = store.BuildWindow(reading.PatientId);
    var prediction = await aiClient.PredictAsync(window, reading, cancellationToken);
    var state = decisionService.Decide(prediction, reading);
    var command = decisionService.ToDeviceCommand(state);

    var latest = new PatientLatestState
    {
        PatientId = reading.PatientId,
        DeviceId = reading.DeviceId,
        State = state,
        Command = command,
        LatestReading = reading,
        Prediction = prediction,
        UpdatedAt = DateTimeOffset.UtcNow,
        IsConnected = true
    };

    store.SetLatest(latest);

    if (state is MonitoringState.Warning or MonitoringState.Seizure &&
        previousState != state)
    {
        store.AddAlert(new PatientAlert
        {
            PatientId = reading.PatientId,
            Type = state == MonitoringState.Warning ? "seizure_warning" : "seizure",
            Title = state == MonitoringState.Warning ? "Possible seizure risk" : "Seizure detected",
            Message = state == MonitoringState.Warning
                ? "AI detected elevated risk within the next 30 minutes."
                : "Emergency seizure state detected by the monitoring pipeline."
        });
    }

    if (state == MonitoringState.Seizure && previousState != MonitoringState.Seizure)
    {
        store.AddEvent(new SeizureEvent
        {
            PatientId = reading.PatientId,
            StartedAt = reading.CapturedAt ?? DateTimeOffset.UtcNow,
            Status = "detected",
            TriggerReading = reading,
            Prediction = prediction
        });
    }

    await hub.Clients.Group(reading.PatientId).SendAsync("patientStateUpdated", latest, cancellationToken);
    await hub.Clients.Group(reading.PatientId).SendAsync("deviceCommand", new
    {
        patientId = reading.PatientId,
        command
    }, cancellationToken);

    return Results.Ok(new IngestReadingResponse
    {
        PatientId = reading.PatientId,
        State = state,
        DeviceCommand = command,
        Prediction = prediction,
        WindowSampleCount = store.GetWindowSampleCount(reading.PatientId)
    });
});

app.MapPost("/api/predictions/run", async (
    AiPredictionRequest request,
    AiPredictionClient aiClient,
    CancellationToken cancellationToken) =>
{
    var latest = new SensorReadingDto
    {
        PatientId = "manual-test",
        DeviceId = "manual",
        State = null,
        CapturedAt = DateTimeOffset.UtcNow
    };

    var prediction = await aiClient.PredictAsync(request, latest, cancellationToken);
    return Results.Ok(prediction);
});

app.MapGet("/api/patients/{patientId}/latest", (
    string patientId,
    MonitoringStore store) =>
{
    var latest = store.GetLatest(patientId);
    if (latest is not null)
    {
        return Results.Ok(latest);
    }

    return Results.Ok(new PatientLatestState
    {
        PatientId = patientId,
        State = MonitoringState.Offline,
        Command = "OFFLINE",
        UpdatedAt = DateTimeOffset.UtcNow,
        IsConnected = false
    });
});

app.MapGet("/api/patients/{patientId}/readings/latest", (
    string patientId,
    int? take,
    MonitoringStore store) =>
{
    return Results.Ok(store.GetRecentReadings(patientId, take ?? 50));
});

app.MapGet("/api/patients/{patientId}/alerts", (
    string patientId,
    MonitoringStore store) => Results.Ok(store.GetAlerts(patientId)));

app.MapGet("/api/patients/{patientId}/seizure-events", (
    string patientId,
    MonitoringStore store) => Results.Ok(store.GetEvents(patientId)));

app.MapGet("/api/seizure/latest", (
    string? patientId,
    MonitoringStore store,
    DecisionService decisionService) =>
{
    var resolvedPatientId = string.IsNullOrWhiteSpace(patientId) ? "demo-patient" : patientId;
    var latest = store.GetLatest(resolvedPatientId);

    if (latest is null)
    {
        return Results.Ok(BuildSeizureLatestResponse(
            new PatientLatestState
            {
                PatientId = resolvedPatientId,
                State = MonitoringState.Offline,
                Command = decisionService.ToDeviceCommand(MonitoringState.Offline),
                UpdatedAt = DateTimeOffset.UtcNow,
                IsConnected = false
            },
            decisionService));
    }

    return Results.Ok(BuildSeizureLatestResponse(latest, decisionService));
});

app.MapHub<PatientMonitoringHub>("/hubs/patient-monitoring");

app.Run();

static object BuildSeizureLatestResponse(
    PatientLatestState latest,
    DecisionService decisionService)
{
    var reading = latest.LatestReading;
    var code = decisionService.ToDeviceCommand(latest.State);

    return new
    {
        prediction = new
        {
            state = decisionService.ToApplicationState(latest.State),
            code,
            probability = decisionService.ToApplicationProbability(latest.State, latest.Prediction)
        },
        sensors = new
        {
            eeg = reading?.Eeg ?? Array.Empty<double>(),
            ecg = reading?.Ecg ?? 0,
            emg = reading?.Emg ?? 0,
            acc = reading?.Acc
        },
        timestamp = reading?.CapturedAt ?? latest.UpdatedAt,
        deviceId = string.IsNullOrWhiteSpace(latest.DeviceId) ? "proteus-01" : latest.DeviceId
    };
}
