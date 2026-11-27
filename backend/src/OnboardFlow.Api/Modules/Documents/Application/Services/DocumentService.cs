using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.Documents.Application.Abstractions;
using OnboardFlow.Api.Modules.Documents.Application.DTOs;
using OnboardFlow.Api.Modules.Documents.Application.Storage;
using OnboardFlow.Api.Modules.Documents.Application.Validation;
using OnboardFlow.Api.Modules.Documents.Domain;
using OnboardFlow.Api.Modules.Documents.Infrastructure;

namespace OnboardFlow.Api.Modules.Documents.Application.Services;

/// <summary>
/// Application service for US-20 document upload and case document metadata listing.
/// </summary>
public sealed class DocumentService : IDocumentService
{
    private readonly DocumentsDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly IFileValidator _fileValidator;
    private readonly ICaseAccess _caseAccess;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditWriter _auditWriter;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        DocumentsDbContext db,
        IFileStorage fileStorage,
        IFileValidator fileValidator,
        ICaseAccess caseAccess,
        ICurrentUser currentUser,
        IAuditWriter auditWriter,
        ILogger<DocumentService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _fileValidator = fileValidator;
        _caseAccess = caseAccess;
        _currentUser = currentUser;
        _auditWriter = auditWriter;
        _logger = logger;
    }

    public async Task<DocumentOperationResult<UploadDocumentResponseDto>> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null || !_currentUser.IsInRole(AppRoles.Client))
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status403Forbidden,
                null,
                "Forbidden",
                "Only clients may upload documents.");
        }

        if (!await _caseAccess.CaseExistsAsync(command.CaseId, ct))
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status404NotFound,
                null,
                "Not Found",
                "Case or checklist item not found.");
        }

        if (!await _caseAccess.CanClientAccessCaseAsync(userId.Value, command.CaseId, ct))
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status403Forbidden,
                null,
                "Forbidden",
                "You do not have access to this case.");
        }

        if (!await _caseAccess.ChecklistItemBelongsToCaseAsync(command.CaseId, command.ChecklistItemId, ct))
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status404NotFound,
                null,
                "Not Found",
                "Case or checklist item not found.");
        }

        if (command.Content is null)
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status400BadRequest,
                "file_missing",
                "File Missing",
                "A file part named 'file' is required.");
        }

        if (command.Length <= 0)
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status400BadRequest,
                "file_empty",
                "File Empty",
                "The selected file is empty.");
        }

        var sanitizedName = FileNameSanitizer.Sanitize(command.OriginalFileName);
        var contentType = string.IsNullOrWhiteSpace(command.ContentType)
            ? "application/octet-stream"
            : command.ContentType.Trim();
        if (contentType.Length > 100)
        {
            contentType = contentType[..100];
        }

        var validation = _fileValidator.Validate(new FileUploadInfo(
            sanitizedName,
            contentType,
            command.Length,
            command.Content));

        if (!validation.IsValid)
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status400BadRequest,
                validation.ErrorCode ?? "file_rejected",
                "File Rejected",
                validation.ErrorMessage ?? "The uploaded file was rejected.");
        }

        var alreadyExists = await _db.Documents
            .AnyAsync(d => d.CaseId == command.CaseId && d.ChecklistItemId == command.ChecklistItemId, ct);
        if (alreadyExists)
        {
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status409Conflict,
                "document_exists",
                "Document Exists",
                "A document was already uploaded for this checklist item.");
        }

        var documentId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var blobName = $"cases/{command.CaseId}/{documentId}/v1/{Guid.NewGuid():N}";

        string sha256;
        long bytesUploaded;
        using (var hashingStream = new HashingReadStream(command.Content))
        {
            await _fileStorage.UploadAsync(blobName, hashingStream, contentType, ct);
            sha256 = hashingStream.GetSha256Hex();
            bytesUploaded = hashingStream.BytesRead > 0 ? hashingStream.BytesRead : command.Length;
        }

        var document = new Document
        {
            Id = documentId,
            CaseId = command.CaseId,
            ChecklistItemId = command.ChecklistItemId,
            CreatedBy = userId.Value,
            CreatedAt = now
        };

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            VersionNumber = 1,
            OriginalFileName = sanitizedName,
            BlobName = blobName,
            ContentType = contentType,
            SizeBytes = bytesUploaded,
            Sha256 = sha256,
            UploadedBy = userId.Value,
            UploadedAt = now
        };

        document.Versions.Add(version);

        try
        {
            await PersistWithAuditAsync(document, userId.Value, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist document metadata for DocumentId {DocumentId}. Compensating by deleting blob {BlobName}.", documentId, blobName);
            await CompensateDeleteBlobAsync(blobName);
            return DocumentOperationResult<UploadDocumentResponseDto>.Fail(
                StatusCodes.Status500InternalServerError,
                "upload_failed",
                "Upload Failed",
                "An unexpected error occurred while saving the document.");
        }

        return DocumentOperationResult<UploadDocumentResponseDto>.Ok(
            StatusCodes.Status201Created,
            new UploadDocumentResponseDto(
                documentId,
                1,
                sanitizedName,
                contentType,
                bytesUploaded,
                now));
    }

    public async Task<DocumentOperationResult<IReadOnlyList<CaseDocumentListItemDto>>> ListForCaseAsync(
        Guid caseId,
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return DocumentOperationResult<IReadOnlyList<CaseDocumentListItemDto>>.Fail(
                StatusCodes.Status401Unauthorized,
                null,
                "Unauthorized",
                "Authentication is required.");
        }

        if (!await _caseAccess.CaseExistsAsync(caseId, ct))
        {
            return DocumentOperationResult<IReadOnlyList<CaseDocumentListItemDto>>.Fail(
                StatusCodes.Status404NotFound,
                null,
                "Not Found",
                "Case not found.");
        }

        var isElevated = _currentUser.IsInRole(AppRoles.Administrator) ||
                         _currentUser.IsInRole(AppRoles.OnboardingManager);

        if (!isElevated)
        {
            if (!_currentUser.IsInRole(AppRoles.Client) ||
                !await _caseAccess.CanClientAccessCaseAsync(userId.Value, caseId, ct))
            {
                return DocumentOperationResult<IReadOnlyList<CaseDocumentListItemDto>>.Fail(
                    StatusCodes.Status403Forbidden,
                    null,
                    "Forbidden",
                    "You do not have access to this case.");
            }
        }

        var items = await _db.Documents
            .AsNoTracking()
            .Where(d => d.CaseId == caseId)
            .Select(d => d.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new CaseDocumentListItemDto(
                    d.Id,
                    d.ChecklistItemId,
                    v.VersionNumber,
                    v.OriginalFileName,
                    v.ContentType,
                    v.SizeBytes,
                    v.UploadedAt,
                    v.UploadedBy))
                .FirstOrDefault())
            .Where(x => x != null)
            .Cast<CaseDocumentListItemDto>()
            .ToListAsync(ct);

        return DocumentOperationResult<IReadOnlyList<CaseDocumentListItemDto>>.Ok(
            StatusCodes.Status200OK,
            items);
    }

    private async Task PersistWithAuditAsync(Document document, Guid actorId, CancellationToken ct)
    {
        if (_db.Database.IsRelational())
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            _db.Documents.Add(document);
            await _db.SaveChangesAsync(ct);
            await _auditWriter.WriteAsync(actorId, "DocumentUploaded", nameof(Document), document.Id, ct);
            await tx.CommitAsync(ct);
        }
        else
        {
            _db.Documents.Add(document);
            await _db.SaveChangesAsync(ct);
            await _auditWriter.WriteAsync(actorId, "DocumentUploaded", nameof(Document), document.Id, ct);
        }
    }

    private async Task CompensateDeleteBlobAsync(string blobName)
    {
        try
        {
            await _fileStorage.DeleteAsync(blobName, CancellationToken.None);
        }
        catch (Exception deleteEx)
        {
            _logger.LogError(deleteEx, "Compensation failed: could not delete orphaned blob {BlobName}.", blobName);
        }
    }
}
