import { useState, useCallback } from 'react';
import MapView from './components/MapView';
import TopBar from './components/TopBar';
import WizardPanel from './components/WizardPanel';
import ScanPanel from './components/ScanPanel';
import ResultsOverlay from './components/ResultsOverlay';
import HistoryDrawer from './components/HistoryDrawer';
import './App.css';

export default function App() {
  const [view, setView] = useState('analyze');
  // step: 1=pick point, 2=params, 3=scanning, 4=results overlay
  const [step, setStep] = useState(1);

  // Map state
  const [selectedPoint, setSelectedPoint] = useState(null);
  const [radius, setRadius] = useState(2000);

  // Form state
  const [dateFrom, setDateFrom] = useState('2020-06-01');
  const [dateTo, setDateTo] = useState('2023-06-01');

  // Analysis state
  const [analysisId, setAnalysisId] = useState(null);
  const [analysisStatus, setAnalysisStatus] = useState(0);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  // Results overlay visibility (separate from step so map stays visible)
  const [showResults, setShowResults] = useState(false);

  // When step becomes 4 → show results overlay
  const handleSetStep = useCallback((newStep) => {
    setStep(newStep);
    if (newStep === 4) {
      setShowResults(true);
    }
  }, []);

  const handleMapClick = useCallback((lat, lng) => {
    if (step > 2) return;
    setSelectedPoint({ lat, lng });
    if (step === 1) setStep(2);
  }, [step]);

  const handleReset = useCallback(() => {
    setStep(1);
    setShowResults(false);
    setSelectedPoint(null);
    setRadius(2000);
    setDateFrom('2020-06-01');
    setDateTo('2023-06-01');
    setAnalysisId(null);
    setAnalysisStatus(0);
    setResult(null);
    setError(null);
  }, []);

  const handleCloseResults = useCallback(() => {
    // Hide overlay but keep the map with circle visible
    setShowResults(false);
  }, []);

  return (
    <div className="app">
      <TopBar view={view} setView={setView} onReset={handleReset} />

      <div className="app-body">
        <MapView
          selectedPoint={selectedPoint}
          radius={radius}
          onMapClick={handleMapClick}
          step={step}
        />

        {view === 'analyze' && (
          <>
            {/* Step 1 & 2: Wizard panel */}
            <WizardPanel
              step={step}
              setStep={setStep}
              selectedPoint={selectedPoint}
              radius={radius}
              setRadius={setRadius}
              dateFrom={dateFrom}
              setDateFrom={setDateFrom}
              dateTo={dateTo}
              setDateTo={setDateTo}
              setAnalysisId={setAnalysisId}
              setAnalysisStatus={setAnalysisStatus}
              setResult={setResult}
              setError={setError}
            />

            {/* Step 3: Scanning panel (small, top-right) */}
            {step === 3 && (
              <ScanPanel
                analysisId={analysisId}
                analysisStatus={analysisStatus}
                setAnalysisStatus={setAnalysisStatus}
                setResult={setResult}
                setStep={handleSetStep}
              />
            )}

            {/* Step 4: Fullscreen results overlay */}
            {showResults && (
              <ResultsOverlay
                result={result}
                error={error}
                analysisStatus={analysisStatus}
                onClose={handleCloseResults}
                onReset={handleReset}
              />
            )}
          </>
        )}

        {view === 'history' && (
          <HistoryDrawer onClose={() => setView('analyze')} />
        )}
      </div>
    </div>
  );
}