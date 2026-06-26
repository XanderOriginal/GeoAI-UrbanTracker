import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { api } from '../services/api';
import './HistoryDrawer.css';

export default function HistoryDrawer({ onClose }) {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchHistory = async () => {
      try {
        const data = await api.getAllAnalyses();
        setHistory(data);
      } catch (err) {
        setError(err.message || 'Не вдалося завантажити історію. Перевірте сервер.');
      } finally {
        setLoading(false);
      }
    };
    fetchHistory();
  }, []);

  const formatDate = (dateString) => {
    if (!dateString) return '';
    return new Date(dateString).toLocaleString('uk-UA', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  };

  // Конвертуємо числові статуси з C# enum у зрозумілий текст
  const getStatusInfo = (status) => {
    switch (status) {
      case 0: return { text: 'В черзі', class: 'pending' };
      case 1: return { text: 'Завантаження фото', class: 'pending' };
      case 2: return { text: 'Обробка', class: 'pending' };
      case 3: return { text: 'Аналіз ШІ', class: 'pending' };
      case 4: return { text: 'Готово', class: 'success' };
      case 5: return { text: 'Помилка', class: 'error' };
      default: return { text: 'Невідомо', class: 'pending' };
    }
  };

  return (
    <motion.div
      className="history-drawer-overlay"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      onClick={onClose}
    >
      <motion.div
        className="history-drawer"
        initial={{ x: '100%' }}
        animate={{ x: 0 }}
        exit={{ x: '100%' }}
        transition={{ type: 'spring', damping: 25, stiffness: 200 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="hd-header">
          <h2>Історія аналізів</h2>
          <button className="hd-close" onClick={onClose}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="hd-body">
          {loading ? (
            <div className="hd-loading">
              <span className="btn-spinner" /> Завантаження...
            </div>
          ) : error ? (
            <div className="hd-error">{error}</div>
          ) : history.length === 0 ? (
            <div className="hd-empty">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
                <path d="M20 12V8H6a2 2 0 01-2-2c0-1.1.9-2 2-2h12v4" />
                <path d="M4 6v12c0 1.1.9 2 2 2h14v-4" />
                <path d="M18 22V6" />
              </svg>
              <p>Історія порожня</p>
            </div>
          ) : (
            <div className="hd-list">
              {history.map((item) => {
                const statusInfo = getStatusInfo(item.status);
                return (
                  <div key={item.requestId} className="hd-card">
                    <div className="hd-card-header">
                      <span className="hd-date">{formatDate(item.createdAt)}</span>
                      <span className={`hd-status ${statusInfo.class}`}>
                        {statusInfo.text}
                      </span>
                    </div>
                    <div className="hd-card-results">
                      {item.status === 4 ? (
                        <>
                          <div className="hd-stat">
                            NDVI: {item.ndviChangePercent > 0 ? '+' : ''}{item.ndviChangePercent?.toFixed(1)}%
                          </div>
                          <div className="hd-stat">
                            Забудова: {item.builtUpAreaChangePercent > 0 ? '+' : ''}{item.builtUpAreaChangePercent?.toFixed(1)}%
                          </div>
                        </>
                      ) : item.status === 5 ? (
                        <div className="hd-error-text">{item.errorMessage || "Помилка виконання"}</div>
                      ) : (
                        <div className="hd-pending-text">В процесі обробки...</div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </motion.div>
    </motion.div>
  );
}