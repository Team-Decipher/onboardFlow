import React from 'react';

interface CaseProgressTrackerProps {
  progressPercentage: number;
}

export const CaseProgressTracker: React.FC<CaseProgressTrackerProps> = ({ progressPercentage }) => {
  return (
    <div style={{ width: '100%', margin: '1rem 0' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem', fontSize: '0.875rem' }}>
        <span style={{ color: '#94a3b8' }}>Onboarding Completion</span>
        <span style={{ color: '#38bdf8', fontWeight: 600 }}>{progressPercentage}%</span>
      </div>
      <div style={{
        width: '100%',
        height: '8px',
        backgroundColor: '#1e293b',
        borderRadius: '9999px',
        overflow: 'hidden',
      }}>
        <div
          style={{
            width: `${Math.min(100, Math.max(0, progressPercentage))}%`,
            height: '100%',
            background: 'linear-gradient(90deg, #3b82f6 0%, #22c55e 100%)',
            transition: 'width 0.4s ease-in-out',
          }}
        />
      </div>
    </div>
  );
};
