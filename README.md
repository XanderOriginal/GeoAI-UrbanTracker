# GeoAI UrbanTracker

Geospatial web application for analyzing land use and vegetation changes between two time periods using real Sentinel-2 satellite imagery and Gemini AI Vision.

![Status](https://img.shields.io/badge/status-active-00ff88?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?style=flat-square&logo=react)
![Sentinel-2](https://img.shields.io/badge/Sentinel--2-Copernicus-003247?style=flat-square)
![Gemini](https://img.shields.io/badge/Gemini-2.5%20Flash-4285F4?style=flat-square&logo=google)

---

## Overview

Click on any point on the map, select a time range, and the system fetches Sentinel-2 satellite images for both periods, computes spectral change metrics, and generates an AI-powered analysis report.

---

## Features

- Interactive fullscreen map with click-to-select analysis point
- Real Sentinel-2 L2A imagery fetched from Copernicus Data Space
- 3-tier cloud cover fallback system (≤10% → ≤20% → ≤35%)
- Multi-index spectral analysis — 6 vegetation indices fused via weighted voting (ExG, VARI, GLI, ExGR, MGRVI, RGBVI)
- Welch's t-test for statistical significance of detected changes
- Gemini 2.5 Flash AI report with exponential retry backoff
- Persistent analysis history

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18, Vite, Framer Motion, Leaflet |
| Backend | .NET 10, ASP.NET Core, Entity Framework Core |
| Database | PostgreSQL |
| Satellite Data | Sentinel-2 L2A via Copernicus Data Space |
| AI Analysis | Google Gemini 2.5 Flash |
| Image Processing | SkiaSharp |
| Containerization | Docker + Docker Compose |

---

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with WSL2 backend
- Copernicus Data Space account — [register here](https://dataspace.copernicus.eu/)
- Google Gemini API key — [get here](https://aistudio.google.com/apikey)

### 1. Clone the repository

```bash
git clone https://github.com/your-username/GeoAI-UrbanTracker.git
cd GeoAI-UrbanTracker
```

### 2. Configure credentials

Create `appsettings.Development.json` in `GeoAI.UrbanTracker.Api/`:

```json
{
  "SentinelHub": {
    "ClientId": "YOUR_COPERNICUS_CLIENT_ID",
    "ClientSecret": "YOUR_COPERNICUS_CLIENT_SECRET"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  }
}
```

### 3. Run

```bash
docker-compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:5095 |
| Swagger | http://localhost:5095/swagger |

---

## How It Works

```
User clicks map
      │
      ▼
Select radius + date range
      │
      ▼
POST /api/analysis  ──► Background job starts
      │
      ├─► Sentinel-2 fetch (before period)
      │       └─► Cloud filter fallback chain (10% → 20% → 35%)
      │
      ├─► Sentinel-2 fetch (after period)
      │
      ├─► Spectral diff
      │       ├─► 6-index vegetation fusion
      │       ├─► Built-up / water / bare soil classification
      │       └─► Welch t-test
      │
      └─► Gemini 2.5 Flash vision analysis
              │
              ▼
        Satellite images + metrics + AI report
```

---

## Spectral Analysis

Vegetation change is computed using a weighted fusion of 6 independent indices derived from the RGB channels of Sentinel-2 true-colour images (B04/B03/B02). NIR is not available in the visual export, so all indices are RGB-based approximations.

| Index | Formula | Weight |
|-------|---------|--------|
| ExG | `2g − r − b` | 30% |
| VARI | `(G−R) / (G+R−B)` | 25% |
| GLI | `(2G−R−B) / (2G+R+B)` | 15% |
| ExGR | `ExG − ExR` | 15% |
| MGRVI | `(G²−R²) / (G²+R²)` | 10% |
| RGBVI | `(G²−R·B) / (G²+R·B)` | 5% |

Statistical significance is evaluated with **Welch's t-test**, variance computed via **Welford's online algorithm** in a single pass over pixel data.

---

## Project Structure

```
GeoAI-UrbanTracker/
├── GeoAI.UrbanTracker.Api/
│   ├── Controllers/
│   ├── Services/
│   │   ├── SatelliteImageService.cs
│   │   ├── ImageDiffService.cs
│   │   ├── GeminiAnalysisService.cs
│   │   └── AnalysisOrchestratorService.cs
│   ├── Helpers/ImageProcessing/
│   │   └── NdviCalculator.cs
│   ├── Models/
│   └── Data/
│
└── geoai-urbantracker-frontend/
    └── src/
        ├── components/
        │   ├── MapView.jsx
        │   ├── WizardPanel.jsx
        │   ├── ScanPanel.jsx
        │   ├── ResultsOverlay.jsx
        │   └── HistoryDrawer.jsx
        └── services/
            └── api.js
```

---

## API

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/analysis` | Start a new analysis |
| `GET` | `/api/analysis/{id}` | Poll status and retrieve results |
| `GET` | `/api/analysis` | List all past analyses |

---

## Limitations

- Vegetation indices are computed from RGB only — results are relative comparisons, not calibrated NDVI values
- Sentinel-2 archive starts from mid-2015; earlier dates are not supported
- In heavily clouded regions the fallback search window expands to ±60 days, which may slightly reduce seasonal comparability
- Gemini API occasionally returns 503 under load; the system retries up to 4 times with exponential backoff before saving a result without AI summary

---

## License

MIT © 2026
