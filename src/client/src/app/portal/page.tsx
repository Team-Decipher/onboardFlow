'use client';

import React, { useState } from 'react';
import { StatusBadge } from '../../components/StatusBadge';
import { CaseProgressTracker } from '../../components/CaseProgressTracker';

export default function ClientPortalPage() {
  const [checklist, setChecklist] = useState([
    { id: '1', title: 'Business Registration Certificate', mandatory: true, status: 'Approved', stage: 'Final' },
    { id: '2', title: 'Director Identity Proofs (KYC)', mandatory: true, status: 'Uploaded', stage: 'Legal' },
    { id: '3', title: 'Audited Financial Statements (Last 2 Years)', mandatory: true, status: 'Pending', stage: 'Finance' },
    { id: '4', title: 'Bank Account Verification Details', mandatory: true, status: 'Pending', stage: 'Finance' },
    { id: '5', title: 'Non-Disclosure Agreement (NDA)', mandatory: false, status: 'Approved', stage: 'Final' },
  ]);

  const handleFileUpload = (id: string) => {
    alert(`File selection dialog opened for requirement #${id}`);
  };

  return (
    <div style={{ maxWidth: '960px', margin: '0 auto', padding: '3rem 1.5rem' }}>
      <div style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 700, marginBottom: '0.5rem' }}>Client Onboarding Portal</h1>
        <p style={{ color: '#94a3b8' }}>Case #ONB-2026-0842 • Enterprise Tier Onboarding</p>
      </div>

      <div style={{ padding: '1.5rem', backgroundColor: '#111827', borderRadius: '12px', border: '1px solid #1f2937', marginBottom: '2.5rem' }}>
        <CaseProgressTracker progressPercentage={40} />
      </div>

      <h2 style={{ fontSize: '1.25rem', marginBottom: '1rem', color: '#f8fafc' }}>Requested Documents Checklist</h2>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {checklist.map((item) => (
          <div
            key={item.id}
            style={{
              padding: '1.25rem',
              backgroundColor: '#111827',
              borderRadius: '8px',
              border: '1px solid #1f2937',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.35rem' }}>
                <span style={{ fontWeight: 600, color: '#f1f5f9' }}>{item.title}</span>
                {item.mandatory && (
                  <span style={{ fontSize: '0.7rem', color: '#ef4444', backgroundColor: 'rgba(239, 68, 68, 0.1)', padding: '0.1rem 0.4rem', borderRadius: '4px' }}>
                    Mandatory
                  </span>
                )}
              </div>
              <p style={{ fontSize: '0.8rem', color: '#64748b' }}>Current review pipeline: {item.stage}</p>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
              <StatusBadge status={item.status} />
              {item.status === 'Pending' && (
                <button
                  onClick={() => handleFileUpload(item.id)}
                  style={{
                    padding: '0.5rem 1rem',
                    backgroundColor: '#3b82f6',
                    color: '#ffffff',
                    border: 'none',
                    borderRadius: '6px',
                    fontSize: '0.85rem',
                    cursor: 'pointer',
                    fontWeight: 500,
                  }}
                >
                  Upload File
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
