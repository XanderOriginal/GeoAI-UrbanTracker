# 🛰️ GeoAI UrbanTracker

> Satellite-powered urban change analysis using Sentinel-2 imagery and Gemini AI Vision.

![Status](https://img.shields.io/badge/status-active-00ff88?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?style=flat-square&logo=react)
![Sentinel-2](https://img.shields.io/badge/Sentinel--2-Copernicus-003247?style=flat-square)
![Gemini](https://img.shields.io/badge/Gemini-2.5%20Flash-4285F4?style=flat-square&logo=google)

---

## 🌍 Overview

**GeoAI UrbanTracker** is a full-stack geospatial web application that analyzes land use and vegetation changes between two time periods using real Sentinel-2 satellite imagery from the Copernicus Data Space.

Click on any point on the map → select a time range → the system fetches satellite images, computes spectral change metrics, and generates an AI-powered urban analysis report.

---

## ✨ Features

- 🗺️ **Interactive fullscreen map** — click anywhere to select an analysis point
- 🛰️ **Real Sentinel-2 L2A imagery** — fetched directly from Copernicus Data Space Ecosystem
- ☁️ **Smart cloud filtering** — 3-tier fallback system (≤10% → ≤20% → ≤35% cloud cover)
- 📊 **Multi-index spectral analysis** — 6 vegetation indices fused via weighted voting (ExG, VARI, GLI, ExGR, MGRVI, RGBVI)
- 📈 **Statistical significance testing** — Welch's t-test with Welford online variance
- 🤖 **Gemini 2.5 Flash AI analysis** — automated urban change interpretation with retry logic
- 🕓 **Analysis history** — persistent archive of all past analyses
- ⚡ **Async background processing** — non-blocking analysis pipeline with real-time status polling

---

## 🖥️ Tech Stack

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

## 🚀 Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with WSL2 backend
- Copernicus Data Space account → [register here](https://dataspace.copernicus.eu/)
- Google Gemini API key → [get here](https://aistudio.google.com/apikey)

### 1. Clone the repository

```bash
git clone https://github.com/your-username/GeoAI-UrbanTracker.git
cd GeoAI-UrbanTracker
```

### 2. Configure environment variables

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

### 3. Run with Docker Compose

```bash
docker-compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:5095 |
| Swagger | http://localhost:5095/swagger |

---

## 📖 How It Works

```
User clicks map
      │
      ▼
Select radius + date range
      │
      ▼
POST /api/analysis  ──► Background job starts
      │
      ├─► Sentinel-2 image fetch (before)
      │       └─► Cloud filter fallback chain (10% → 20% → 35%)
      │
      ├─► Sentinel-2 image fetch (after)
      │
      ├─► Spectral diff analysis
      │       ├─► 6-index vegetation fusion (ExG, VARI, GLI, ExGR, MGRVI, RGBVI)
      │       ├─► Built-up / water / bare soil classification
      │       └─► Welch t-test for statistical significance
      │
      └─► Gemini 2.5 Flash vision analysis (with retry backoff)
              │
              ▼
        Results displayed with satellite images + metrics + AI report
```

---

## 📊 Spectral Analysis

The app computes vegetation change using a **weighted fusion of 6 independent indices** — all derived from the RGB channels of Sentinel-2 true-colour images (B04/B03/B02), since NIR is not available in the visual export.

| Index | Formula | Weight |
|-------|---------|--------|
| ExG | `2g − r − b` | 30% |
| VARI | `(G−R) / (G+R−B)` | 25% |
| GLI | `(2G−R−B) / (2G+R+B)` | 15% |
| ExGR | `ExG − ExR` | 15% |
| MGRVI | `(G²−R²) / (G²+R²)` | 10% |
| RGBVI | `(G²−R·B) / (G²+R·B)` | 5% |

Statistical significance of change is evaluated using **Welch's t-test** computed via **Welford's online algorithm** (single-pass, numerically stable).

---

## 🗂️ Project Structure

```
GeoAI-UrbanTracker/
├── GeoAI.UrbanTracker.Api/        # .NET backend
│   ├── Controllers/               # REST API endpoints
│   ├── Services/                  # Business logic
│   │   ├── SatelliteImageService  # Sentinel-2 image fetching
│   │   ├── ImageDiffService       # Spectral analysis
│   │   ├── GeminiAnalysisService  # AI integration
│   │   └── AnalysisOrchestrator   # Pipeline coordinator
│   ├── Models/                    # Domain entities
│   ├── Helpers/ImageProcessing/   # NdviCalculator
│   └── Data/                      # EF Core DbContext
│
└── geoai-urbantracker-frontend/   # React frontend
    └── src/
        ├── components/
        │   ├── MapView            # Leaflet map
        │   ├── WizardPanel        # Analysis setup wizard
        │   ├── ScanPanel          # Progress tracking
        │   ├── ResultsOverlay     # Results display
        │   └── HistoryDrawer      # Past analyses
        └── services/
            └── api.js             # API client
```

---

## 📡 API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/analysis` | Start a new analysis |
| `GET` | `/api/analysis/{id}` | Poll analysis status & results |
| `GET` | `/api/analysis` | Get all past analyses |

---

## ⚠️ Limitations

- Vegetation indices are computed from **RGB only** (no NIR band) — results are comparative, not absolute NDVI
- Sentinel-2 coverage starts from **mid-2015** — older dates will fail
- Heavily clouded regions may fall back to wider search windows (up to ±60 days), slightly affecting seasonal comparability
- Gemini API may return 503 under high load — the system retries up to 4 times with exponential backoff

---

## 📄 License

MIT © 2026
