import { useState, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import MapView from './components/MapView';
import TopBar from './components/TopBar';
import WizardPanel from './components/WizardPanel';
import ScanPanel from './components/ScanPanel';
import ResultsOverlay from './components/ResultsOverlay';
import HistoryDrawer from './components/HistoryDrawer';
import './App.css';

export default function App() {
  const [view, setView] = useState('analyze');
  const [step, setStep] = useState(1);

  const [selectedPoint, setSelectedPoint] = useState(null);
  const [radius, setRadius] = useState(2000);

  const [dateFrom, setDateFrom] = useState('2020-06-01');
  const [dateTo, setDateTo] = useState('2023-06-01');

  const [analysisId, setAnalysisId] = useState(null);
  const [analysisStatus, setAnalysisStatus] = useState(0);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const [showResults, setShowResults] = useState(false);

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

        <AnimatePresence>
          {step === 1 && (
            <motion.div
              className="map-hint-global"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: 10 }}
              transition={{ delay: 0.5 }}
            >
              <div className="map-hint-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M12 22s-8-4.5-8-11.8A8 8 0 0112 2a8 8 0 018 8.2c0 7.3-8 11.8-8 11.8z"/>
                  <circle cx="12" cy="10" r="3"/>
                </svg>
              </div>
              <span>Click anywhere on the map to select analysis point</span>
            </motion.div>
          )}
        </AnimatePresence>

        {view === 'analyze' && (
          <>
            <WizardPanel
              step={step}
              setStep={handleSetStep}
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

            {step === 3 && (
              <ScanPanel
                analysisId={analysisId}
                analysisStatus={analysisStatus}
                setAnalysisStatus={setAnalysisStatus}
                setResult={setResult}
                setStep={handleSetStep}
              />
            )}

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