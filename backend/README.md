# EpiCare Backend

.NET 8 backend for the EpiCare Medical IoT + AI monitoring flow.

## Architecture

```text
Proteus / Arduino HEX
  -> Virtual Serial COM
  -> EpiCare.IoTBridge local console app
  -> EpiCare.Api deployed backend
  -> Railway AI model API
  -> Flutter app endpoints + SignalR realtime hub
```

## Projects

```text
src/EpiCare.Api
  Public backend API for Flutter and the IoT bridge.

src/EpiCare.IoTBridge
  Local console app that reads Proteus serial JSON and posts it to the backend.
```

## AI Model

Configured by environment variables:

```text
AiModel__BaseUrl=https://seizeit2-api-production.up.railway.app
AiModel__PredictPath=/predict
AiModel__WindowSize=240
AiModel__ModelWindowCount=8
AiModel__ModelSamplesPerWindow=3840
AiModel__ModelEegChannels=2
AiModel__ModelEcgChannels=1
AiModel__ModelEmgChannels=1
AiModel__HighRiskThreshold=0.7
AiModel__TimeoutSeconds=60
Decision__DemoMode=false
Decision__AccGravityBaseline=980
Decision__AccVerticalScale=50
Decision__AccMovementThreshold=2.5
```

The model request is:

```text
EEG shape: (8, 2, 3840)
ECG shape: (8, 1, 3840)
EMG shape: (8, 1, 3840)
```

The backend accepts Proteus single-sample readings and builds the model window internally, then posts the payload to `POST /predict` on the configured SeizeIT2 API server. To move the AI model to a new server, change `AiModel__BaseUrl` only.

## API Endpoints for Flutter

Replace `{baseUrl}` with your deployed URL.

```http
GET  {baseUrl}/api/health
GET  {baseUrl}/api/seizure/latest
GET  {baseUrl}/api/patients/demo-patient/latest
GET  {baseUrl}/api/patients/demo-patient/readings/latest?take=50
GET  {baseUrl}/api/patients/demo-patient/alerts
GET  {baseUrl}/api/patients/demo-patient/seizure-events
POST {baseUrl}/api/predictions/run
```

Realtime:

```text
{baseUrl}/hubs/patient-monitoring
```

SignalR client should call:

```text
JoinPatient("demo-patient")
```

Events emitted:

```text
patientStateUpdated
deviceCommand
```

## IoT Bridge Input

In normal AI-driven mode, Proteus/Arduino sends raw sensor data without a state label:

```json
{"eeg":[502,498],"ecg":72,"emg":18,"acc":[0.1,-0.1,980]}
{"eeg":[610,548],"ecg":64,"emg":220,"acc":[2.1,-1.4,940]}
```

Set Arduino `SEND_DEMO_STATE=true` and backend `Decision__DemoMode=true` only when a deterministic Proteus demo must force `N`, `P`, or `S`.

The bridge adds:

```json
{
  "patientId": "demo-patient",
  "deviceId": "proteus-simulator",
  "capturedAt": "..."
}
```

## Run Locally

Install .NET 8 SDK first.

```powershell
cd backend
dotnet restore
dotnet run --project src/EpiCare.Api
```

Backend runs on the URL printed by .NET, usually:

```text
http://localhost:5080
```

Test health:

```powershell
Invoke-WebRequest -Uri "http://localhost:5080/api/health" -UseBasicParsing
```

## Run IoT Bridge

Run Proteus as administrator, set up Virtual Serial Port Kit, then:

```powershell
cd backend
dotnet run --project src/EpiCare.IoTBridge -- --port COM3 --backend http://localhost:5080 --patient demo-patient
```

For deployed backend:

```powershell
dotnet run --project src\EpiCare.IoTBridge -- --port COM3 --backend https://epicare-api-production.up.railway.app --patient demo-patient
```

If the Arduino sketch is reading commands back from serial, enable closed-loop output:

```powershell
dotnet run --project src\EpiCare.IoTBridge -- --port COM3 --backend https://epicare-api-production.up.railway.app --patient demo-patient --write-command true
```

## Deploy to Render

1. Push this repository to GitHub.
2. Open Render.
3. Create a new Blueprint or Web Service.
4. If using Blueprint, select `backend/render.yaml`.
5. If using manual Web Service:
   - Runtime: Docker
   - Root directory: `backend`
   - Dockerfile path: `src/EpiCare.Api/Dockerfile`
   - Health check path: `/api/health`

After deploy, give Flutter this base URL:

```text
https://YOUR-RENDER-SERVICE.onrender.com
```

## Deploy to Railway

Use the Dockerfile builder, not Railpack. If Railway reports `railpack process exited with an error`, open the Railway service settings and set:

```text
Builder: Dockerfile
Dockerfile Path: Dockerfile
Build Command: empty
Start Command: empty
```

The backend automatically binds to Railway's `PORT` variable.

## Notes

- Current storage is in-memory for fast demo deployment. It resets when the free server sleeps/restarts.
- Add PostgreSQL later for permanent history.
- Production/default mode ignores embedded `state` values. AI high risk produces `P`; high risk plus abnormal ACC movement produces `S`.
- A direct AI `Seizure` / `Ictal` label also produces `S`.
- `Decision__DemoMode=true` allows simulator `state: "P"` / `state: "S"` to force the result for controlled demonstrations.
- AI HTTP failures return a neutral unavailable result and never copy simulator state into the prediction.
