FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/src/EpiCare.Api/EpiCare.Api.csproj backend/src/EpiCare.Api/
RUN dotnet restore backend/src/EpiCare.Api/EpiCare.Api.csproj

COPY backend/src/EpiCare.Api/ backend/src/EpiCare.Api/
RUN dotnet publish backend/src/EpiCare.Api/EpiCare.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

ENTRYPOINT ["dotnet", "EpiCare.Api.dll"]
