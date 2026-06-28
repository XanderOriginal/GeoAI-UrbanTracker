import { useEffect, useRef } from 'react';
import { motion } from 'framer-motion';
import { api, STATUS } from '../services/api';
import CircularProgress from './CircularProgress';
import './ScanPanel.css';

const POLL_INTERVAL = 2500;

export default function ScanPanel({ analysisId, analysisStatus, setAnalysisStatus, setResult, setStep }) {
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
      } catch (err) { console.error('Poll error:', err); }
    };
    poll();
    timerRef.current = setInterval(poll, POLL_INTERVAL);
    return () => clearInterval(timerRef.current);
  }, [analysisId, analysisStatus, setAnalysisStatus, setResult, setStep]);

  const statusInfo = STATUS[analysisStatus] ?? STATUS[0];

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

      <div className="scan-panel-header">
        <div className="scan-panel-title">SCANNING</div>
        <div className="scan-panel-status">{statusInfo.label}</div>
      </div>

      <div className="scan-divider" />

      <CircularProgress analysisStatus={analysisStatus} />

      <div className="scan-divider" />

      <div className="scan-footer">
        <span className="scan-id-label">REQUEST ID</span>
        <span className="scan-id-val">{String(analysisId).slice(0, 8).toUpperCase()}…</span>
      </div>
    </motion.div>
  );
}