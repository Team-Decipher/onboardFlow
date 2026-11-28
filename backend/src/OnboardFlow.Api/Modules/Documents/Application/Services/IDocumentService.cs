using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OnboardFlow.Api.Modules.Documents.Application.DTOs;

namespace OnboardFlow.Api.Modules.Documents.Application.Services;

public sealed record UploadDocumentCommand(
    Guid CaseId,
    Guid ChecklistItemId,
    string? OriginalFileName,
    string? ContentType,
    long Length,
    Stream? Content);

public interface IDocumentService
{
    Task<DocumentOperationResult<UploadDocumentResponseDto>> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken ct = default);

    Task<DocumentOperationResult<IReadOnlyList<CaseDocumentListItemDto>>> ListForCaseAsync(
        Guid caseId,
        CancellationToken ct = default);
}
