import './TopBar.css';

export default function TopBar({ view, setView, onReset }) {
  return (
    <header className="topbar">
      <div className="topbar-logo">
        <div className="logo-icon">
          <svg viewBox="0 0 24 24" fill="none">
            <polygon points="12,2 22,7 22,17 12,22 2,17 2,7" stroke="#00ff88" strokeWidth="1.5" fill="none"/>
            <circle cx="12" cy="12" r="3" fill="#00ff88"/>
            <line x1="12" y1="5" x2="12" y2="9" stroke="#00ff88" strokeWidth="1.5"/>
            <line x1="12" y1="15" x2="12" y2="19" stroke="#00ff88" strokeWidth="1.5"/>
            <line x1="5" y1="12" x2="9" y2="12" stroke="#00ff88" strokeWidth="1.5"/>
            <line x1="15" y1="12" x2="19" y2="12" stroke="#00ff88" strokeWidth="1.5"/>
          </svg>
        </div>
        <div className="logo-text">
          <span className="logo-name">GEOAI</span>
          <span className="logo-sub">URBANTRACKER</span>
        </div>
        <span className="logo-badge">v1.0</span>
      </div>

      <nav className="topbar-nav">
        <button
          className={`nav-btn ${view === 'analyze' ? 'active' : ''}`}
          onClick={() => setView('analyze')}
        >
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/>
            <path fillRule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10z" clipRule="evenodd"/>
          </svg>
          АНАЛІЗ
        </button>
        <button
          className={`nav-btn ${view === 'history' ? 'active' : ''}`}
          onClick={() => setView('history')}
        >
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-13a.75.75 0 00-1.5 0v5c0 .414.336.75.75.75h4a.75.75 0 000-1.5h-3.25V5z" clipRule="evenodd"/>
          </svg>
          ІСТОРІЯ
        </button>
      </nav>

      <div className="topbar-right">
        <div className="status-indicator">
          <div className="status-dot" />
          <span className="status-text">SENTINEL-2 ONLINE</span>
        </div>
      </div>
    </header>
  );
}