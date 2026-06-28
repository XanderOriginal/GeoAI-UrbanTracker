import { useState, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { api } from '../services/api';
import './WizardPanel.css';

const MIN_DATE = '2015-07-01';
const MAX_DATE = new Date().toISOString().split('T')[0];

const RADIUS_OPTIONS = [
  { value: 500,   label: '500M' },
  { value: 1000,  label: '1KM' },
  { value: 2000,  label: '2KM' },
  { value: 5000,  label: '5KM' },
  { value: 10000, label: '10KM' },
  { value: 25000, label: '25KM' },
];

export default function WizardPanel({
  step, setStep,
  selectedPoint, radius, setRadius,
  dateFrom, setDateFrom, dateTo, setDateTo,
  setAnalysisId, setAnalysisStatus, setResult, setError,
}) {
  const [loading, setLoading] = useState(false);

  const dateErrors = useMemo(() => {
    const errors = {};
    if (dateFrom < MIN_DATE) errors.dateFrom = `Мін: ${MIN_DATE}`;
    if (dateFrom > MAX_DATE) errors.dateFrom = 'Не може бути в майбутньому';
    if (dateTo > MAX_DATE) errors.dateTo = 'Не може бути в майбутньому';
    if (dateTo <= dateFrom) errors.dateTo = 'Має бути пізніше початкової';
    const diff = (new Date(dateTo) - new Date(dateFrom)) / (1000*60*60*24*30);
    if (diff < 1) errors.dateTo = 'Мін. діапазон — 1 місяць';
    return errors;
  }, [dateFrom, dateTo]);

  const canRun = selectedPoint && Object.keys(dateErrors).length === 0;

  const handleRun = async () => {
    if (!canRun) return;
    setLoading(true);
    setError(null);
    try {
      const data = await api.createAnalysis({
        latitude: selectedPoint.lat,
        longitude: selectedPoint.lng,
        radiusMeters: radius,
        dateFrom,
        dateTo,
      });
      setAnalysisId(data.requestId);
      setAnalysisStatus(0);
      setStep(3);
    } catch (err) {
      setError(err.message || 'Помилка підключення до сервера');
    } finally {
      setLoading(false);
    }
  };

  if (step >= 3) return null;

  return (
    <AnimatePresence mode="wait">
      <motion.div
        className="wizard-panel"
        key="wizard"
        initial={{ opacity: 0, x: -30 }}
        animate={{ opacity: 1, x: 0 }}
        exit={{ opacity: 0, x: -30 }}
        transition={{ type: 'spring', stiffness: 300, damping: 28 }}
      >
        {/* Corner decorations */}
        <div className="corner corner-tl" />
        <div className="corner corner-tr" />
        <div className="corner corner-bl" />
        <div className="corner corner-br" />

        {/* Step bar */}
        <div className="wizard-steps-bar">
          {[
            { n: 1, label: 'ЛОКАЦІЯ' },
            { n: 2, label: 'ПАРАМЕТРИ' },
            { n: 3, label: 'РЕЗУЛЬТАТ' },
          ].map((s, i, arr) => (
            <div key={s.n} style={{ display: 'flex', alignItems: 'center', flex: i < arr.length - 1 ? '1' : 'none' }}>
              <div className={`ws-step ${step > s.n ? 'done' : ''} ${step === s.n ? 'active' : ''}`}>
                <div className="ws-dot">
                  {step > s.n ? '✓' : s.n}
                </div>
                <span>{s.label}</span>
              </div>
              {i < arr.length - 1 && (
                <div className={`ws-line ${step > s.n ? 'filled' : ''}`} />
              )}
            </div>
          ))}
        </div>

        <AnimatePresence mode="wait">
          {step === 1 && (
            <motion.div
              key="step1"
              className="wizard-body"
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -12 }}
              transition={{ duration: 0.2 }}
            >
              <div className="step1-hero">
                <div className="radar-wrap">
                  <div className="radar-circle radar-c1" />
                  <div className="radar-circle radar-c2" />
                  <div className="radar-circle radar-c3" />
                  <div className="radar-sweep" />
                  <div className="radar-dot" />
                </div>
              </div>

              <h2 className="wizard-title">CHOOSE A LOCATION</h2>
              <p className="wizard-desc">
                Click on the map to select an analysis point. Sentinel-2 covers the entire Earth's surface every 5 days.
              </p>

              
            </motion.div>
          )}

          {step === 2 && (
            <motion.div
              key="step2"
              className="wizard-body"
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -12 }}
              transition={{ duration: 0.2 }}
            >
              {/* Coords badge */}
              <div className="coords-badge">
                <svg viewBox="0 0 16 16" fill="currentColor" style={{ width: 12, height: 12, color: 'var(--neon)', flexShrink: 0 }}>
                  <path fillRule="evenodd" d="M8 0C5.24 0 3 2.24 3 5c0 3.75 5 11 5 11s5-7.25 5-11c0-2.76-2.24-5-5-5zm0 7a2 2 0 110-4 2 2 0 010 4z"/>
                </svg>
                <span className="coords-text">
                  {selectedPoint?.lat.toFixed(5)}°N &nbsp;{selectedPoint?.lng.toFixed(5)}°E
                </span>
                <button className="coords-change" onClick={() => setStep(1)}>ЗМІНИТИ</button>
              </div>

              {/* Radius */}
              <div className="field-group">
                <div className="field-header">
                  <span className="field-label">RADIUS</span>
                  <span className="field-val">{radius >= 1000 ? `${radius/1000} км` : `${radius} м`}</span>
                </div>
                <div className="radius-grid">
                  {RADIUS_OPTIONS.map(opt => (
                    <button
                      key={opt.value}
                      className={`radius-btn ${radius === opt.value ? 'active' : ''}`}
                      onClick={() => setRadius(opt.value)}
                    >
                      {opt.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Dates */}
              <div className="field-group">
                <div className="field-header">
                  <span className="field-label">TIME RANGE</span>
                </div>
                <div className="date-row">
                  <div className="date-col">
                    <span className="date-tag">FROM</span>
                    <input
                      type="date"
                      className={`date-input ${dateErrors.dateFrom ? 'err' : ''}`}
                      value={dateFrom}
                      min={MIN_DATE} max={MAX_DATE}
                      onChange={e => setDateFrom(e.target.value)}
                    />
                    {dateErrors.dateFrom && <span className="date-err">{dateErrors.dateFrom}</span>}
                  </div>
                  <svg viewBox="0 0 20 20" fill="currentColor" className="date-arrow">
                    <path fillRule="evenodd" d="M10.293 5.293a1 1 0 011.414 0l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414-1.414L12.586 11H5a1 1 0 110-2h7.586l-2.293-2.293a1 1 0 010-1.414z"/>
                  </svg>
                  <div className="date-col">
                    <span className="date-tag">TO</span>
                    <input
                      type="date"
                      className={`date-input ${dateErrors.dateTo ? 'err' : ''}`}
                      value={dateTo}
                      min={dateFrom || MIN_DATE} max={MAX_DATE}
                      onChange={e => setDateTo(e.target.value)}
                    />
                    {dateErrors.dateTo && <span className="date-err">{dateErrors.dateTo}</span>}
                  </div>
                </div>
                
              </div>

              {/* Note */}
              <div className="wizard-note">
                <span className="note-tag">INFO</span>
                Processing 15–30 sec.
              </div>

              {/* Run button */}
              <motion.button
                className={`run-btn ${!canRun ? 'disabled' : ''}`}
                onClick={handleRun}
                disabled={loading || !canRun}
                whileHover={canRun && !loading ? { scale: 1.01 } : {}}
                whileTap={canRun && !loading ? { scale: 0.99 } : {}}
              >
                {loading ? (
                  <>
                    <div className="btn-spinner" />
                    LAUNCHING...
                  </>
                ) : !canRun ? (
                  <>
                    <svg viewBox="0 0 20 20" fill="currentColor" style={{ width: 16, height: 16 }}>
                      <path fillRule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 6a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 6zm0 9a1 1 0 100-2 1 1 0 000 2z"/>
                    </svg>
                    FIX THE ERRORS
                  </>
                ) : (
                  <>
                    <svg viewBox="0 0 20 20" fill="currentColor" style={{ width: 16, height: 16 }}>
                      <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z"/>
                    </svg>
                    RUN ANALYSIS
                  </>
                )}
              </motion.button>
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>
    </AnimatePresence>
  );
}