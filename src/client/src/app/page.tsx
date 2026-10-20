import React from 'react';

export default function HomePage() {
  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '4rem 2rem' }}>
      <section style={{ textAlign: 'center', marginBottom: '4rem' }}>
        <h1 style={{
          fontSize: '3rem',
          fontWeight: 800,
          marginBottom: '1.5rem',
          background: 'linear-gradient(135deg, #ffffff 0%, #94a3b8 100%)',
          WebkitBackgroundClip: 'text',
          WebkitTextFillColor: 'transparent',
          letterSpacing: '-0.03em'
        }}>
          Automated Client Onboarding & Review Pipelines
        </h1>
        <p style={{ fontSize: '1.2rem', color: '#94a3b8', maxWidth: '700px', margin: '0 auto 2.5rem' }}>
          Streamline statutory document collection, multi-stage approval routing, and compliance tracking without email chaos.
        </p>
        <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
          <a
            href="/portal"
            style={{
              padding: '0.8rem 1.8rem',
              backgroundColor: '#3b82f6',
              color: '#ffffff',
              borderRadius: '8px',
              fontWeight: 600,
              display: 'inline-block'
            }}
          >
            Launch Client Portal
          </a>
          <a
            href="/dashboard"
            style={{
              padding: '0.8rem 1.8rem',
              backgroundColor: '#1e293b',
              color: '#f8fafc',
              border: '1px solid #334155',
              borderRadius: '8px',
              fontWeight: 600,
              display: 'inline-block'
            }}
          >
            Manager Dashboard
          </a>
        </div>
      </section>

      <section style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
        gap: '2rem',
        marginTop: '3rem'
      }}>
        <div style={{ padding: '2rem', backgroundColor: '#111827', borderRadius: '12px', border: '1px solid #1f2937' }}>
          <h3 style={{ fontSize: '1.25rem', marginBottom: '0.75rem', color: '#38bdf8' }}>Dynamic Checklists</h3>
          <p style={{ color: '#94a3b8', lineHeight: 1.6 }}>
            Automatically generate document requirements based on client tiers (Enterprise, SME, Partner) with due-date enforcement.
          </p>
        </div>
        <div style={{ padding: '2rem', backgroundColor: '#111827', borderRadius: '12px', border: '1px solid #1f2937' }}>
          <h3 style={{ fontSize: '1.25rem', marginBottom: '0.75rem', color: '#a855f7' }}>Sequential Approvals</h3>
          <p style={{ color: '#94a3b8', lineHeight: 1.6 }}>
            Route uploaded files through Legal, Finance, and Executive reviewers with granular audit logging and feedback loops.
          </p>
        </div>
        <div style={{ padding: '2rem', backgroundColor: '#111827', borderRadius: '12px', border: '1px solid #1f2937' }}>
          <h3 style={{ fontSize: '1.25rem', marginBottom: '0.75rem', color: '#22c55e' }}>Secure Cloud Storage</h3>
          <p style={{ color: '#94a3b8', lineHeight: 1.6 }}>
            Retain full version history and protect sensitive KYC / business files using Azure Blob Storage and AES-256 encryption.
          </p>
        </div>
      </section>
    </div>
  );
}
