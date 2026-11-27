using System;
using System.Threading;
using System.Threading.Tasks;

namespace OnboardFlow.Api.Modules.Documents.Contracts;

/// <summary>
/// Read-only document metadata exposed by the Documents module to other modules.
/// </summary>
public sealed record DocumentMetadataDto(
    Guid DocumentId,
    Guid CaseId,
    Guid ChecklistItemId,
    int CurrentVersionNumber,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt,
    Guid UploadedBy);

/// <summary>
/// Public contract for other modules to query document metadata without accessing file bytes or storage URLs.
/// </summary>
public interface IDocumentReader
{
    Task<DocumentMetadataDto?> GetDocumentForItemAsync(Guid caseId, Guid checklistItemId, CancellationToken ct = default);
}
