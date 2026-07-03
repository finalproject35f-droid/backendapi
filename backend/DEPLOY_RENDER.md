# Deploy EpiCare API to Render

## 1. Push to GitHub

Create a GitHub repository, then from the project root:

```powershell
git init
git add .
git commit -m "Add EpiCare backend and Flutter scaffold"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO.git
git push -u origin main
```

If the repo already exists locally, only run:

```powershell
git add .
git commit -m "Add EpiCare backend deploy setup"
git push
```

## 2. Create Render Web Service

Render settings:

```text
Runtime: Docker
Root Directory: backend
Dockerfile Path: src/EpiCare.Api/Dockerfile
Health Check Path: /api/health
Plan: Free
```

Environment variables:

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

## 3. After Deploy

Open:

```text
https://YOUR-RENDER-SERVICE.onrender.com
https://YOUR-RENDER-SERVICE.onrender.com/swagger
https://YOUR-RENDER-SERVICE.onrender.com/api/health
```

Give Flutter developer:

```text
Base URL: https://YOUR-RENDER-SERVICE.onrender.com
Docs: https://YOUR-RENDER-SERVICE.onrender.com/swagger
Contract: backend/FLUTTER_INTEGRATION.md
```
