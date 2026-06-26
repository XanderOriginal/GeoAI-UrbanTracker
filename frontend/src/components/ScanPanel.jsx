import { useEffect, useRef } from 'react';
import { motion } from 'framer-motion';
import { api, STATUS } from '../services/api';
import './ScanPanel.css';

const POLL_INTERVAL = 2500;

const STEPS = [
  { id: 1, label: 'Завантаження Sentinel-2' },
  { id: 2, label: 'Аналіз пікселів / NDVI' },
  { id: 3, label: 'Gemini Vision AI' },
  { id: 4, label: 'Збереження результатів' },
];

export default function ScanPanel({
  analysisId,
  analysisStatus,
  setAnalysisStatus,
  setResult,
  setStep,
}) {
  const timerRef = useRef(null);

  useEffect(() => {
    if (!analysisId || analysisStatus >= 4) return;

    const poll = async () => {
      try {
        const data = await api.getAnalysis(analysisId);
        const status = typeof data.status === 'number' ? data.status : 0;
        setAnalysisStatus(status);

        if (status === 4 || status === 5) {
          setResult(data);
          setStep(4);
          clearInterval(timerRef.current);
        }
      } catch (err) {
        console.error('Poll error:', err);
      }
    };

    poll();
    timerRef.current = setInterval(poll, POLL_INTERVAL);
    return () => clearInterval(timerRef.current);
  }, [analysisId, analysisStatus, setAnalysisStatus, setResult, setStep]);

  const currentStatusInfo = STATUS[analysisStatus] ?? STATUS[0];

  return (
    <motion.div
      className="scan-panel"
      initial={{ opacity: 0, x: 30 }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ type: 'spring', stiffness: 280, damping: 26 }}
    >
      <div className="corner corner-tl" />
      <div className="corner corner-tr" />
      <div className="corner corner-bl" />
      <div className="corner corner-br" />

      <div className="scan-header">
        <div className="scan-spinners">
          <div className="spin-ring spin-r1" />
          <div className="spin-ring spin-r2" />
          <div className="spin-core" />
        </div>
        <div className="scan-header-text">
          <div className="scan-label">АНАЛІЗ ВИКОНУЄТЬСЯ</div>
          <div className="scan-status">{currentStatusInfo.label}</div>
        </div>
      </div>

      <div className="scan-divider" />

      <div className="scan-steps">
        {STEPS.map(ps => {
          const done   = currentStatusInfo.step > ps.id;
          const active = currentStatusInfo.step === ps.id;
          return (
            <div key={ps.id} className={`scan-step ${done ? 'done' : ''} ${active ? 'active' : ''}`}>
              <div className="ss-icon">
                {done   ? <span>✓</span>
                : active ? <div className="ss-pulse" />
                :          <span>{ps.id}</span>}
              </div>
              <span className="ss-label">{ps.label}</span>
              {active && (
                <div className="ss-bar">
                  <div className="ss-bar-fill" />
                </div>
              )}
            </div>
          );
        })}
      </div>

      <div className="scan-divider" />

      <div className="scan-footer">
        <span className="scan-id-label">REQUEST ID</span>
        <span className="scan-id-val">{String(analysisId).slice(0, 8).toUpperCase()}…</span>
      </div>
    </motion.div>
  );
}