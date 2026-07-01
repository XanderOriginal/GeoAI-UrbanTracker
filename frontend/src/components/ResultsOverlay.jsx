import { motion, AnimatePresence } from 'framer-motion';
import './ResultsOverlay.css';



function Metric({ label, value, unit = '%', positiveIsBad }) {
  const num = typeof value === 'number' ? value : null;
  const display = num !== null
    ? `${num > 0 ? '+' : ''}${num.toFixed(2)}${unit}`
    : '—';

  let color = 'var(--text-muted)';
  if (num !== null && num !== 0) {
    if (positiveIsBad) {
      color = num > 0 ? 'var(--red)' : 'var(--neon)';
    } else {
      color = num > 0 ? 'var(--neon)' : 'var(--red)';
    }
  }

  return (
    <div className="ro-metric">
      <div className="ro-metric-label">{label}</div>
      <div className="ro-metric-value" style={{ color }}>
        {display}
      </div>
      <div className="ro-metric-bar">
        <div
          className="ro-metric-bar-fill"
          style={{
            width: num !== null ? `${Math.min(Math.abs(num) * 3, 100)}%` : '0%',
            background: color,
            boxShadow: `0 0 8px ${color}`,
          }}
        />
      </div>
    </div>
  );
}

export default function ResultsOverlay({ result, error, analysisStatus, onClose, onReset }) {
  const isError = analysisStatus === 5 || !!error;
  const isSuccess = analysisStatus === 4 && result && !error;

  return (
    <AnimatePresence>
      <motion.div
        className="ro-backdrop"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.4 }}
      >
        {/* Blurred map background handled via CSS backdrop-filter */}

        <motion.div
          className="ro-container"
          initial={{ opacity: 0, scale: 0.96, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.96, y: 20 }}
          transition={{ type: 'spring', stiffness: 260, damping: 24, delay: 0.1 }}
        >
          {/* Corner decorations */}
          <div className="corner corner-tl" />
          <div className="corner corner-tr" />
          <div className="corner corner-bl" />
          <div className="corner corner-br" />

          {/* Header */}
          <div className="ro-header">
            <div className="ro-header-left">
              {isSuccess ? (
                <>
                  <div className="ro-status-dot success" />
                  <span className="ro-header-title">ANALYSIS COMPLETED</span>
                  <span className="ro-header-sub">
                    {result?.completedAt
                      ? new Date(result.completedAt).toLocaleString('uk-UA')
                      : ''}
                  </span>
                </>
              ) : (
                <>
                  <div className="ro-status-dot error" />
                  <span className="ro-header-title error">ANALYSIS FAILED</span>
                </>
              )}
            </div>
            <div className="ro-header-right">
              <span className="ro-request-id">
                ID: {String(result?.requestId || '').slice(0, 8).toUpperCase()}
              </span>
              <button className="ro-close-btn" onClick={onClose} title="Закрити">
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
                </svg>
              </button>
            </div>
          </div>

          <div className="ro-divider" />

          {/* Error state */}
          {isError && (
            <div className="ro-error-body">
              <div className="ro-error-icon">⚠</div>
              <div className="ro-error-title">EXECUTION ERROR</div>
              <div className="ro-error-msg">
                {error || result?.errorMessage || 'Check your server connection or try a different location and dates.'}
              </div>
              <button className="ro-reset-btn" onClick={onReset}>
                <svg viewBox="0 0 20 20" fill="currentColor" style={{ width: 14, height: 14 }}>
                  <path fillRule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H3.989a.75.75 0 00-.75.75v4.242a.75.75 0 001.5 0v-2.43l.31.31a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm1.23-3.723a.75.75 0 00.219-.53V2.929a.75.75 0 00-1.5 0V5.36l-.31-.31A7 7 0 003.239 8.188a.75.75 0 101.448.389A5.5 5.5 0 0113.89 6.11l.311.31h-2.432a.75.75 0 000 1.5h4.243a.75.75 0 00.53-.219z"/>
                </svg>
                NEW ANALYSIS
              </button>
            </div>
          )}

          {/* Success state */}
          {isSuccess && (
            <div className="ro-body">

              {/* === LEFT: Images === */}
              <div className="ro-images-col">
                <div className="ro-img-section-title">SENTINEL-2 SATELLITE IMAGES</div>

                <div className="ro-images-grid">
                  {/* Before */}
                  <div className="ro-img-card">
                    <div className="ro-img-header before">
                      <span className="ro-img-tag">BEFORE</span>
                      <span className="ro-img-date">{result.dateFrom || ''}</span>
                    </div>
                    {result.beforeImageUrl ? (
                      <img
                        src={result.beforeImageUrl}
                        alt="Before analysis"
                        className="ro-img"
                        onError={e => {
                          e.target.style.display = 'none';
                          e.target.nextSibling.style.display = 'flex';
                        }}
                      />
                    ) : null}
                    <div className="ro-img-fallback" style={{ display: result.beforeImageUrl ? 'none' : 'flex' }}>
                      <span>Snapshot unavailable</span>
                    </div>
                  </div>

                  {/* After */}
                  <div className="ro-img-card">
                    <div className="ro-img-header after">
                      <span className="ro-img-tag">AFTER</span>
                      <span className="ro-img-date">{result.dateTo || ''}</span>
                    </div>
                    {result.afterImageUrl ? (
                      <img
                        src={result.afterImageUrl}
                        alt="After analysis"
                        className="ro-img"
                        onError={e => {
                          e.target.style.display = 'none';
                          e.target.nextSibling.style.display = 'flex';
                        }}
                      />
                    ) : null}
                    <div className="ro-img-fallback" style={{ display: result.afterImageUrl ? 'none' : 'flex' }}>
                      <span>Snapshot unavailable</span>
                    </div>
                  </div>
                </div>

                {/* Location info */}
                {result.latitude && (
                  <div className="ro-location-info">
                    <span className="ro-loc-tag">COORDINATES</span>
                    <span className="ro-loc-val">
                      {result.latitude?.toFixed(5)}°N &nbsp; {result.longitude?.toFixed(5)}°E
                    </span>
                    <span className="ro-loc-tag" style={{ marginLeft: 12 }}>RADIUS</span>
                    <span className="ro-loc-val">
                      {result.radiusMeters >= 1000
                        ? `${(result.radiusMeters / 1000).toFixed(1)} км`
                        : `${result.radiusMeters} м`}
                    </span>
                  </div>
                )}
              </div>

              <div className="ro-col-divider" />

              {/* === RIGHT: Metrics + AI === */}
              <div className="ro-data-col">

                {/* Metrics */}
                <div className="ro-metrics-section">
                  <div className="ro-section-title">
                    <span className="ro-section-tag">METRICS</span>
                    CHANGES PER PERIOD
                  </div>

                  <div className="ro-metrics-grid">
                    <Metric
                      label="CONSTRUCTION"
                      value={result.builtUpAreaChangePercent}
                      positiveIsBad={true}
                    />
                    <Metric
                      label="GREEN AREAS"
                      value={result.greenAreaChangePercent}
                      positiveIsBad={false}
                    />
                    <Metric
                      label="NDVI INDEX"
                      value={result.ndviChangePercent}
                      positiveIsBad={false}
                    />
                  </div>

                  {/* Explanation */}
                  <div className="ro-metrics-legend">
                    <div className="legend-item">
                      <div className="legend-dot" style={{ background: 'var(--red)' }} />
                      <span>Negative change</span>
                    </div>
                    <div className="legend-item">
                      <div className="legend-dot" style={{ background: 'var(--neon)' }} />
                      <span>Positive change</span>
                    </div>
                    <div className="legend-item">
                      <div className="legend-dot" style={{ background: 'var(--text-muted)' }} />
                      <span>No changes</span>
                    </div>
                  </div>
                </div>

                <div className="ro-divider" />

                {/* Gemini AI */}
                {result.geminiSummary && (
                  <div className="ro-ai-section">
                    <div className="ro-section-title">
                      <span className="ro-section-tag cyan">AI</span>
                      GEMINI VISION ANALYSIS
                      <span className="ro-ai-model">gemini-2.5-flash</span>
                    </div>
                    <div className="ro-ai-text">
                      {result.geminiSummary
                        .split(/\*\*([^*]+)\*\*/g)
                        .map((part, i) =>
                          i % 2 === 1
                            ? <strong key={i} style={{ color: 'var(--neon)', fontWeight: 600 }}>{part}</strong>
                            : part.split('\n').map((line, j) =>
                                <span key={`${i}-${j}`}>{line}{j < part.split('\n').length - 1 && <br />}</span>
                              )
                        )}
                    </div>
                  </div>
                )}

                <div className="ro-divider" />

                {/* Actions */}
                <div className="ro-actions">
                  <button className="ro-reset-btn" onClick={onReset}>
                    <svg viewBox="0 0 20 20" fill="currentColor" style={{ width: 14, height: 14 }}>
                      <path fillRule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H3.989a.75.75 0 00-.75.75v4.242a.75.75 0 001.5 0v-2.43l.31.31a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm1.23-3.723a.75.75 0 00.219-.53V2.929a.75.75 0 00-1.5 0V5.36l-.31-.31A7 7 0 003.239 8.188a.75.75 0 101.448.389A5.5 5.5 0 0113.89 6.11l.311.31h-2.432a.75.75 0 000 1.5h4.243a.75.75 0 00.53-.219z"/>
                    </svg>
                    NEW ANALYSIS
                  </button>
                  <button className="ro-close-action-btn" onClick={onClose}>
                    RETURN TO MAP
                  </button>
                </div>
              </div>
            </div>
          )}
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}