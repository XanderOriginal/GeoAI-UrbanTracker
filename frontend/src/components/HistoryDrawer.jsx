import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { api } from '../services/api';
import ResultsOverlay from './ResultsOverlay';
import './HistoryDrawer.css';

export default function HistoryDrawer({ onClose }) {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedResult, setSelectedResult] = useState(null);

  useEffect(() => {
    const fetchHistory = async () => {
      try {
        const data = await api.getAllAnalyses();
        setHistory(data);
      } catch (err) {
        setError(err.message || 'Failed to load history. Check server connection.');
      } finally {
        setLoading(false);
      }
    };
    fetchHistory();
  }, []);

  const formatDate = (dateString) => {
    if (!dateString) return '';
    return new Date(dateString).toLocaleString('en-GB', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  };

  const getStatusInfo = (status) => {
    switch (status) {
      case 0: return { text: 'QUEUED',     cls: 'pending' };
      case 1: return { text: 'FETCHING',   cls: 'pending' };
      case 2: return { text: 'PROCESSING', cls: 'pending' };
      case 3: return { text: 'AI ANALYSIS',cls: 'pending' };
      case 4: return { text: 'COMPLETED',  cls: 'success' };
      case 5: return { text: 'FAILED',     cls: 'error'   };
      default: return { text: 'UNKNOWN',   cls: 'pending' };
    }
  };

  return (
    <>
      <motion.div
        className="hd-overlay"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
      >
        <motion.div
          className="hd-drawer"
          initial={{ x: '100%' }}
          animate={{ x: 0 }}
          exit={{ x: '100%' }}
          transition={{ type: 'spring', damping: 25, stiffness: 200 }}
          onClick={e => e.stopPropagation()}
        >
          <div className="hd-header">
            <div className="hd-header-left">
              <div className="hd-title-tag">ARCHIVE</div>
              <h2 className="hd-title">Analysis History</h2>
            </div>
            <button className="hd-close" onClick={onClose}>
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
              </svg>
            </button>
          </div>

          <div className="hd-divider" />

          <div className="hd-body">
            {loading && (
              <div className="hd-state">
                <div className="hd-spinner" />
                <span>Loading...</span>
              </div>
            )}

            {error && (
              <div className="hd-state error">
                <span>⚠ {error}</span>
              </div>
            )}

            {!loading && !error && history.length === 0 && (
              <div className="hd-state">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"
                     style={{ width: 40, height: 40, opacity: 0.3 }}>
                  <path d="M9 12h6m-3-3v6M3 12a9 9 0 1118 0 9 9 0 01-18 0z"/>
                </svg>
                <span>No analyses yet</span>
              </div>
            )}

            {!loading && !error && history.length > 0 && (
              <div className="hd-list">
                {history.map(item => {
                  const si = getStatusInfo(item.status);
                  const clickable = item.status === 4;
                  return (
                    <div
                      key={item.requestId}
                      className={`hd-card ${clickable ? 'clickable' : ''}`}
                      onClick={() => clickable && setSelectedResult(item)}
                    >
                      {clickable && (
                        <>
                          <div className="hdc-corner hdc-tl" />
                          <div className="hdc-corner hdc-br" />
                        </>
                      )}

                      <div className="hdc-top">
                        <span className="hdc-date">{formatDate(item.createdAt)}</span>
                        <span className={`hdc-status ${si.cls}`}>{si.text}</span>
                      </div>

                      {item.status === 4 && (
                        <>
                          <div className="hdc-coords">
                            {item.latitude?.toFixed(4)}°N &nbsp; {item.longitude?.toFixed(4)}°E
                          </div>
                          <div className="hdc-metrics">
                            <div className="hdc-metric">
                              <span className="hdc-m-label">BUILT-UP</span>
                              <span className="hdc-m-val" style={{
                                color: item.builtUpAreaChangePercent > 0 ? 'var(--red)' : 'var(--neon)'
                              }}>
                                {item.builtUpAreaChangePercent > 0 ? '+' : ''}
                                {item.builtUpAreaChangePercent?.toFixed(1)}%
                              </span>
                            </div>
                            <div className="hdc-metric">
                              <span className="hdc-m-label">GREEN</span>
                              <span className="hdc-m-val" style={{
                                color: item.greenAreaChangePercent < 0 ? 'var(--red)' : 'var(--neon)'
                              }}>
                                {item.greenAreaChangePercent > 0 ? '+' : ''}
                                {item.greenAreaChangePercent?.toFixed(1)}%
                              </span>
                            </div>
                            <div className="hdc-metric">
                              <span className="hdc-m-label">NDVI</span>
                              <span className="hdc-m-val" style={{ color: 'var(--cyan)' }}>
                                {item.ndviChangePercent > 0 ? '+' : ''}
                                {item.ndviChangePercent?.toFixed(1)}%
                              </span>
                            </div>
                          </div>
                          <div className="hdc-open-hint">
                            <svg viewBox="0 0 16 16" fill="currentColor"
                                 style={{ width: 10, height: 10 }}>
                              <path d="M6.22 8.72a.75.75 0 001.06 1.06l4.25-4.25V8a.75.75 0 001.5 0V4a.75.75 0 00-.75-.75h-4a.75.75 0 000 1.5h2.47L6.22 8.72z"/>
                            </svg>
                            VIEW FULL RESULTS
                          </div>
                        </>
                      )}

                      {item.status === 5 && (
                        <div className="hdc-error-msg">
                          {item.errorMessage || 'Analysis failed'}
                        </div>
                      )}

                      {item.status < 4 && (
                        <div className="hdc-pending-msg">In progress...</div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </motion.div>
      </motion.div>

      <AnimatePresence>
        {selectedResult && (
          <ResultsOverlay
            result={selectedResult}
            error={null}
            analysisStatus={selectedResult.status}
            onClose={() => setSelectedResult(null)}
            onReset={() => { setSelectedResult(null); onClose(); }}
          />
        )}
      </AnimatePresence>
    </>
  );
}