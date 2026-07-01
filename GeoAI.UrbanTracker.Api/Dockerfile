FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["GeoAI.UrbanTracker.Api.csproj", "."]
RUN dotnet restore "GeoAI.UrbanTracker.Api.csproj"
COPY . .
RUN dotnet publish "GeoAI.UrbanTracker.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GeoAI.UrbanTracker.Api.dll"]