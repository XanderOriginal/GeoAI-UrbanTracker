const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5095';

export const api = {
  async createAnalysis({ latitude, longitude, radiusMeters, dateFrom, dateTo }) {
    const res = await fetch(`${BASE_URL}/api/analysis`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ latitude, longitude, radiusMeters, dateFrom, dateTo }),
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
  },

  async getAnalysis(id) {
    const res = await fetch(`${BASE_URL}/api/analysis/${id}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
  },

  async getAllAnalyses() {
    const res = await fetch(`${BASE_URL}/api/analysis`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
  },

  imageUrl(path) {
    if (!path) return null;
    const filename = path.split(/[/\\]/).pop();
    return `${BASE_URL}/images/${filename}`;
  },
};

// Status enum (matches backend AnalysisStatus)
export const STATUS = {
  0: { label: 'Pending', step: 0 },
  1: { label: 'Fetching Images', step: 1 },
  2: { label: 'Processing', step: 2 },
  3: { label: 'Analyzing with AI', step: 3 },
  4: { label: 'Completed', step: 4 },
  5: { label: 'Failed', step: -1 },
};