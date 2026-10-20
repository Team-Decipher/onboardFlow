import React from 'react';

export const Header: React.FC = () => {
  return (
    <header style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '1rem 2rem',
      backgroundColor: '#0f172a',
      color: '#ffffff',
      borderBottom: '1px solid #1e293b'
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
        <div style={{
          width: '32px',
          height: '32px',
          borderRadius: '8px',
          background: 'linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontWeight: 'bold',
          fontSize: '1.1rem'
        }}>
          O
        </div>
        <span style={{ fontSize: '1.25rem', fontWeight: 700, letterSpacing: '-0.025em' }}>
          onboard<span style={{ color: '#3b82f6' }}>Flow</span>
        </span>
      </div>
      <nav style={{ display: 'flex', alignItems: 'center', gap: '1.5rem', fontSize: '0.9rem' }}>
        <a href="/" style={{ color: '#94a3b8', textDecoration: 'none' }}>Overview</a>
        <a href="/portal" style={{ color: '#94a3b8', textDecoration: 'none' }}>Client Portal</a>
        <a href="/dashboard" style={{ color: '#94a3b8', textDecoration: 'none' }}>Manager Dashboard</a>
      </nav>
    </header>
  );
};
