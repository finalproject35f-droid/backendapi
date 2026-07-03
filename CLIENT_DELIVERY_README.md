# EpiCare Client Delivery

## Contents

- `backend/`: EpiCare .NET 8 API, IoT Bridge, configuration, and deployment files.
- `arduino/sketch_may19a.ino`: updated Proteus/Arduino sensor and actuator sketch.
- `documentation/EpiCare_Backend_AI_Integration_Report.docx`: client technical report.
- `documentation/BACKEND_AI_FLOW_REPORT_AR.md`: Arabic implementation summary.

## Default production behavior

- AI model API: `AiModel__BaseUrl=https://seizeit2-api-production.up.railway.app`
- To move the model to a new server, update `AiModel__BaseUrl` in environment variables and keep `AiModel__PredictPath=/predict`.
- Backend: `Decision__DemoMode=false`
- Arduino: `SEND_DEMO_STATE=false`
- Start IoT Bridge with `--write-command true` when N/P/S must be returned to Arduino.

## Required verification before release

Install .NET 8 SDK and run:

```powershell
cd backend
dotnet build EpiCare.Backend.sln
```

The ACC calibration values currently target the Proteus simulation where resting Z is approximately 980. Recalibrate them before using a physical accelerometer with different units.
