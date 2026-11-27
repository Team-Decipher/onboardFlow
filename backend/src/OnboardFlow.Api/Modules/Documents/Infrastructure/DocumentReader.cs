using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Modules.Documents.Contracts;

namespace OnboardFlow.Api.Modules.Documents.Infrastructure;

public sealed class DocumentReader : IDocumentReader
{
    private readonly DocumentsDbContext _db;

    public DocumentReader(DocumentsDbContext db)
    {
        _db = db;
    }

    public async Task<DocumentMetadataDto?> GetDocumentForItemAsync(
        Guid caseId,
        Guid checklistItemId,
        CancellationToken ct = default)
    {
        return await _db.Documents
            .AsNoTracking()
            .Where(d => d.CaseId == caseId && d.ChecklistItemId == checklistItemId)
            .Select(d => d.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new DocumentMetadataDto(
                    d.Id,
                    d.CaseId,
                    d.ChecklistItemId,
                    v.VersionNumber,
                    v.OriginalFileName,
                    v.ContentType,
                    v.SizeBytes,
                    v.UploadedAt,
                    v.UploadedBy))
                .FirstOrDefault())
            .FirstOrDefaultAsync(ct);
    }
}
