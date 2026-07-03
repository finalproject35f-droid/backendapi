# Deploy EpiCare API to Railway

## Recommended Setup

Use the repository root as the Railway service root.

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
