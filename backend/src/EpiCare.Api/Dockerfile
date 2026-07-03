FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/EpiCare.Api/EpiCare.Api.csproj src/EpiCare.Api/
RUN dotnet restore src/EpiCare.Api/EpiCare.Api.csproj

COPY src/EpiCare.Api/ src/EpiCare.Api/
RUN dotnet publish src/EpiCare.Api/EpiCare.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

ENTRYPOINT ["dotnet", "EpiCare.Api.dll"]
