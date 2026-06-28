import { useEffect, useState } from 'react';
import './CircularProgress.css';

const STEPS = [
  { id: 1, label: 'Fetching Sentinel-2 imagery',  short: 'FETCH'    },
  { id: 2, label: 'Pixel diff & NDVI analysis',   short: 'PROCESS'  },
  { id: 3, label: 'Gemini 2.5 Flash Vision AI',   short: 'AI'       },
  { id: 4, label: 'Saving results to database',   short: 'SAVE'     },
];

const TOTAL_STEPS = STEPS.length;

// Backend status → completed steps count
// Status 0 (Pending/Queued) — analysis just started, show step 1 as active
const statusToProgress = {
  0: 0,  // queued — nothing done yet, step 1 will be active
  1: 0,  // fetching — step 1 is active (not yet done)
  2: 1,  // processing — step 1 done, step 2 active
  3: 2,  // AI analysis — step 2 done, step 3 active
  4: 4,  // completed — all done
  5: 0,  // failed
};

// Which step is currently active (0 = none)
const statusToActiveStep = {
  0: 1,
  1: 1,
  2: 2,
  3: 3,
  4: 0,
  5: 0,
};

function CircularProgressRing({ progress, total, size = 120, stroke = 6 }) {
  const radius = (size - stroke * 2) / 2;
  const circumference = 2 * Math.PI * radius;
  const pct = Math.min(progress / total, 1);
  const offset = circumference - pct * circumference;

  return (
    <svg
      width={size}
      height={size}
      className="cp-svg"
      style={{ transform: 'rotate(-90deg)' }}
    >
      <circle
        cx={size / 2}
        cy={size / 2}
        r={radius}
        fill="none"
        stroke="rgba(0,255,136,0.08)"
        strokeWidth={stroke}
      />
      <circle
        cx={size / 2}
        cy={size / 2}
        r={radius}
        fill="none"
        stroke="url(#neonGradient)"
        strokeWidth={stroke}
        strokeLinecap="round"
        strokeDasharray={circumference}
        strokeDashoffset={offset}
        className="cp-arc"
        style={{ transition: 'stroke-dashoffset 0.8s cubic-bezier(0.4,0,0.2,1)' }}
      />
      <circle
        cx={size / 2}
        cy={size / 2}
        r={radius}
        fill="none"
        stroke="rgba(0,255,136,0.15)"
        strokeWidth={stroke + 6}
        strokeLinecap="round"
        strokeDasharray={circumference}
        strokeDashoffset={offset}
        style={{ transition: 'stroke-dashoffset 0.8s cubic-bezier(0.4,0,0.2,1)', filter: 'blur(3px)' }}
      />
      <defs>
        <linearGradient id="neonGradient" x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%"   stopColor="#00ffcc" />
          <stop offset="100%" stopColor="#00ff88" />
        </linearGradient>
      </defs>
    </svg>
  );
}

export default function CircularProgress({ analysisStatus }) {
  const completedSteps = statusToProgress[analysisStatus] ?? 0;
  const activeStep = statusToActiveStep[analysisStatus] ?? 1;

  // displayStep animates from 0 → completedSteps for the ring fill
  const [displayStep, setDisplayStep] = useState(0);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    if (completedSteps > displayStep) {
      const t = setTimeout(() => setDisplayStep(s => s + 1), 100);
      return () => clearTimeout(t);
    }
  }, [completedSteps, displayStep]);

  useEffect(() => {
    const i = setInterval(() => setTick(t => t + 1), 1200);
    return () => clearInterval(i);
  }, []);

  // Ring shows completed steps + partial for current active
  // e.g. status=1 (fetching): 0 done, step 1 active → ring at 0/4 but shows indeterminate on step 1
  // We show completedSteps on the ring (the "done" portion)
  const ringProgress = displayStep;
  const pct = analysisStatus === 4
    ? 100
    : Math.round(((displayStep + 0.5) / TOTAL_STEPS) * 100); // +0.5 so active step shows partial fill

  return (
    <div className="cp-wrapper">
      <div className="cp-ring-wrap">
        <CircularProgressRing
          progress={analysisStatus === 4 ? TOTAL_STEPS : displayStep + (activeStep > 0 ? 0.5 : 0)}
          total={TOTAL_STEPS}
          size={140}
          stroke={5}
        />

        <div className="cp-center">
          <div className="cp-pct">
            {pct}<span className="cp-pct-sym">%</span>
          </div>
          <div className="cp-step-label">
            {analysisStatus === 4
              ? `${TOTAL_STEPS}/${TOTAL_STEPS} STEPS`
              : `${displayStep}/${TOTAL_STEPS} STEPS`
            }
          </div>
        </div>

        {activeStep > 0 && (
          <div
            className="cp-orbit-dot"
            style={{
              '--orbit-angle': `${((displayStep + 0.5) / TOTAL_STEPS) * 360 - 90}deg`,
            }}
          />
        )}
      </div>

      <div className="cp-steps">
        {STEPS.map(step => {
          const done   = completedSteps >= step.id;
          const active = !done && activeStep === step.id;
          const future = !done && !active;
          return (
            <div
              key={step.id}
              className={`cp-step ${done ? 'done' : ''} ${active ? 'active' : ''} ${future ? 'future' : ''}`}
            >
              <div className="cp-step-icon">
                {done ? (
                  <svg viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                    <path d="M2 6l3 3 5-5"/>
                  </svg>
                ) : active ? (
                  <div className="cp-step-pulse" />
                ) : (
                  <span>{step.id}</span>
                )}
              </div>

              <div className="cp-step-body">
                <span className="cp-step-short">{step.short}</span>
                <span className="cp-step-full">{step.label}</span>
              </div>

              {active && (
                <div className="cp-step-bar">
                  <div className="cp-step-bar-fill" />
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}