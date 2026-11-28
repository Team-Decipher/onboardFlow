"use client";

import React, { useRef, useState } from "react";
import { uploadDocumentWithProgress } from "../api";
import { UploadDocumentResponse } from "../types";

type UploadState = "empty" | "selected" | "uploading" | "success" | "error";

interface DocumentUploadPanelProps {
  caseId: string;
  itemId: string;
  itemName?: string;
  onSuccess?: (response: UploadDocumentResponse) => void;
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

export function DocumentUploadPanel({
  caseId,
  itemId,
  itemName,
  onSuccess,
}: DocumentUploadPanelProps) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [state, setState] = useState<UploadState>("empty");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [progress, setProgress] = useState<number>(0);
  const [errorMessage, setErrorMessage] = useState<string>("");
  const [uploadedDoc, setUploadedDoc] = useState<UploadDocumentResponse | null>(
    null
  );
  const [isDragging, setIsDragging] = useState(false);

  const handleFileChosen = (file: File | null) => {
    if (!file) return;
    setSelectedFile(file);
    setErrorMessage("");
    setUploadedDoc(null);
    setProgress(0);
    setState("selected");
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files?.[0] ?? null;
    handleFileChosen(file);
  };

  const handleUpload = async () => {
    if (!selectedFile) return;
    setState("uploading");
    setErrorMessage("");
    setProgress(0);

    try {
      const response = await uploadDocumentWithProgress(
        caseId,
        itemId,
        selectedFile,
        (pct) => setProgress(pct)
      );
      setProgress(100);
      setUploadedDoc(response);
      setState("success");
      onSuccess?.(response);
    } catch (err) {
      const msg =
        err instanceof Error
          ? err.message
          : "Upload failed. Please try again.";
      setErrorMessage(msg);
      setState("error");
    }
  };

  const handleReset = () => {
    setSelectedFile(null);
    setErrorMessage("");
    setUploadedDoc(null);
    setProgress(0);
    setState("empty");
    if (inputRef.current) {
      inputRef.current.value = "";
    }
  };

  return (
    <div className="w-full max-w-2xl rounded-2xl bg-[#0d272d] border border-[#18434d] p-5 sm:p-7 text-slate-100 shadow-xl">
      <div className="mb-5">
        <p className="text-xs uppercase tracking-widest text-cyan-400 font-semibold">
          Upload Document
        </p>
        <h2 className="text-lg sm:text-xl font-bold text-white mt-1">
          {itemName || "Checklist Document"}
        </h2>
        <p className="text-xs text-slate-400 mt-1">
          Files are encrypted in transit and stored in private storage.
        </p>
      </div>

      {/* Drop Zone */}
      <div
        role="region"
        aria-label="File drop zone"
        onDragOver={(e) => {
          e.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        className={`rounded-xl border-2 border-dashed p-6 sm:p-8 text-center transition-colors ${
          isDragging
            ? "border-cyan-400 bg-[#12353d]"
            : "border-[#255763] bg-[#0a1f24] hover:border-cyan-500/70"
        }`}
      >
        <input
          ref={inputRef}
          type="file"
          aria-label="Choose file"
          className="hidden"
          onChange={(e) => handleFileChosen(e.target.files?.[0] ?? null)}
        />
        <p className="text-sm text-slate-200 font-medium">
          Drag and drop your file here
        </p>
        <p className="text-xs text-slate-400 mt-1 mb-4">
          or select a file from your device
        </p>
        <button
          type="button"
          disabled={state === "uploading"}
          onClick={() => inputRef.current?.click()}
          className="px-4 py-2 rounded-lg bg-[#143d46] hover:bg-[#1a4f5b] text-cyan-200 text-xs font-semibold transition-colors disabled:opacity-50"
        >
          Browse files
        </button>
      </div>

      {/* Selected File Card */}
      {selectedFile && (
        <div className="mt-4 rounded-xl bg-[#0a1f24] border border-[#1a4650] p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div className="min-w-0">
            <p className="text-sm font-medium text-white truncate">
              {selectedFile.name}
            </p>
            <p className="text-xs text-slate-400 mt-0.5">
              {formatBytes(selectedFile.size)} &middot;{" "}
              {selectedFile.type || "application/octet-stream"}
            </p>
          </div>
          {state !== "uploading" && state !== "success" && (
            <button
              type="button"
              onClick={handleReset}
              className="text-xs text-slate-400 hover:text-white self-start sm:self-center"
            >
              Remove
            </button>
          )}
        </div>
      )}

      {/* Progress Bar */}
      {state === "uploading" && (
        <div className="mt-4" role="progressbar" aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100}>
          <div className="flex justify-between text-xs text-slate-300 mb-1.5">
            <span>Uploading...</span>
            <span>{progress}%</span>
          </div>
          <div className="w-full h-2 rounded-full bg-[#0a1f24] overflow-hidden">
            <div
              className="h-full bg-cyan-400 transition-all duration-150"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      )}

      {/* Error Banner */}
      {state === "error" && errorMessage && (
        <div
          role="alert"
          className="mt-4 rounded-xl bg-red-950/60 border border-red-700/60 px-4 py-3 text-xs text-red-200 flex items-center justify-between gap-3"
        >
          <span>{errorMessage}</span>
          <button
            type="button"
            onClick={handleUpload}
            className="px-3 py-1.5 rounded-lg bg-red-800/70 hover:bg-red-700 text-white font-semibold shrink-0"
          >
            Retry
          </button>
        </div>
      )}

      {/* Success Banner */}
      {state === "success" && uploadedDoc && (
        <div
          role="status"
          className="mt-4 rounded-xl bg-emerald-950/60 border border-emerald-700/60 px-4 py-3 text-xs text-emerald-200"
        >
          <p className="font-semibold text-emerald-100">
            Document uploaded successfully (Version {uploadedDoc.versionNumber})
          </p>
          <p className="mt-1 text-emerald-300">
            {uploadedDoc.fileName} &middot; {formatBytes(uploadedDoc.sizeBytes)}
          </p>
        </div>
      )}

      {/* Action Footer */}
      <div className="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
        <button
          type="button"
          disabled={!selectedFile || state === "uploading" || state === "success"}
          onClick={handleUpload}
          className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-cyan-500 hover:bg-cyan-400 text-slate-950 font-semibold text-sm transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {state === "uploading" ? "Uploading..." : "Upload document"}
        </button>
      </div>
    </div>
  );
}
