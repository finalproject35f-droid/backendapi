# Deploy EpiCare API to Railway

## Recommended Setup

Use the repository root as the Railway service root.

Important: choose **Dockerfile** as the Railway builder. Do not use Railpack for this service.
Railway's ASP.NET Core guide requires a Dockerfile for .NET apps because Railpack does not yet support .NET reliably.

Railway will read:

```text
railway.json
Dockerfile
```

The root Dockerfile builds the API from:

```text
backend/src/EpiCare.Api
```

## Environment Variables

Add these in Railway Variables:

```text
ASPNETCORE_ENVIRONMENT=Production
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

When the AI model is redeployed to a different server, update `AiModel__BaseUrl` only and keep `AiModel__PredictPath=/predict`.

Do not set `ASPNETCORE_URLS` manually unless needed. The Dockerfile already uses:

```text
http://+:8080
```

## If You Set Root Directory to backend

That also works now. Railway will read:

```text
backend/railway.json
backend/Dockerfile
```

In this setup, also keep Builder set to **Dockerfile** and Dockerfile Path set to `Dockerfile`.

## If Railway Shows "railpack process exited with an error"

This means Railway is still trying to use Railpack instead of the Dockerfile. Fix it from the Railway dashboard:

1. Open the backend service.
2. Go to **Settings -> Build**.
3. Set **Builder** to **Dockerfile**.
4. If the service root is the repository root, set Dockerfile Path to `Dockerfile`.
5. If the service root is `backend`, set Dockerfile Path to `Dockerfile`.
6. Remove any custom Build Command or Start Command.
7. Redeploy.

The app reads Railway's `PORT` variable automatically, so no manual `ASPNETCORE_URLS` setting is required.

## After Deploy

Generate a public Railway domain, then test:

```text
https://YOUR-SERVICE.up.railway.app/
https://YOUR-SERVICE.up.railway.app/swagger
https://YOUR-SERVICE.up.railway.app/api/health
```

Give Flutter:

```text
Base URL: https://YOUR-SERVICE.up.railway.app
Swagger: https://YOUR-SERVICE.up.railway.app/swagger
```
