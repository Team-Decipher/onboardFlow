using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.Documents.Application.Abstractions;
using OnboardFlow.Api.Modules.Documents.Application.Services;
using OnboardFlow.Api.Modules.Documents.Application.Storage;
using OnboardFlow.Api.Modules.Documents.Application.Validation;
using OnboardFlow.Api.Modules.Documents.Infrastructure;

namespace OnboardFlow.Api.Tests.Documents;

public class DocumentServiceTests
{
    private static DocumentsDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocumentsDbContext(options);
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Blobs { get; } = new();
        public List<string> DeletedBlobs { get; } = new();
        public bool ThrowOnUpload { get; set; }

        public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default)
        {
            if (ThrowOnUpload)
            {
                throw new InvalidOperationException("Simulated blob upload failure.");
            }

            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            Blobs[blobName] = ms.ToArray();
        }

        public Task DeleteAsync(string blobName, CancellationToken ct = default)
        {
            DeletedBlobs.Add(blobName);
            Blobs.Remove(blobName);
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string blobName, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream(Blobs[blobName]));
    }

    private sealed class FakeCaseAccess : ICaseAccess
    {
        public bool Exists { get; set; } = true;
        public bool CanAccess { get; set; } = true;
        public bool ItemBelongs { get; set; } = true;

        public Task<bool> CaseExistsAsync(Guid caseId, CancellationToken ct = default) => Task.FromResult(Exists);
        public Task<bool> CanClientAccessCaseAsync(Guid userId, Guid caseId, CancellationToken ct = default) => Task.FromResult(CanAccess);
        public Task<bool> ChecklistItemBelongsToCaseAsync(Guid caseId, Guid checklistItemId, CancellationToken ct = default) => Task.FromResult(ItemBelongs);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string Role { get; set; } = AppRoles.Client;
        public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.Ordinal);
    }

    private sealed class FakeAuditWriter : IAuditWriter
    {
        public List<(Guid ActorId, string Action, string EntityType, Guid EntityId)> Entries { get; } = new();
        public bool ThrowOnWrite { get; set; }

        public Task WriteAsync(Guid actorId, string action, string entityType, Guid entityId, CancellationToken ct = default)
        {
            if (ThrowOnWrite)
            {
                throw new InvalidOperationException("Simulated DB/audit failure.");
            }
            Entries.Add((actorId, action, entityType, entityId));
            return Task.CompletedTask;
        }
    }

    private sealed class RejectingValidator : IFileValidator
    {
        public FileValidationResult Validate(FileUploadInfo file) =>
            FileValidationResult.Fail("file_rejected", "Blocked by custom validator.");
    }

    private static DocumentService CreateService(
        DocumentsDbContext db,
        FakeFileStorage? storage = null,
        IFileValidator? validator = null,
        FakeCaseAccess? caseAccess = null,
        FakeCurrentUser? currentUser = null,
        FakeAuditWriter? auditWriter = null)
    {
        return new DocumentService(
            db,
            storage ?? new FakeFileStorage(),
            validator ?? new PassThroughFileValidator(),
            caseAccess ?? new FakeCaseAccess(),
            currentUser ?? new FakeCurrentUser(),
            auditWriter ?? new FakeAuditWriter(),
            NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task UploadAsync_EmptyFile_Returns400FileEmpty()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(db);
        using var stream = new MemoryStream();

        var result = await service.UploadAsync(new UploadDocumentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "empty.pdf", "application/pdf", 0, stream));

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("file_empty", result.ErrorCode);
    }

    [Fact]
    public async Task UploadAsync_MissingFile_Returns400FileMissing()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(db);

        var result = await service.UploadAsync(new UploadDocumentCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null, 0, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("file_missing", result.ErrorCode);
    }

    [Fact]
    public async Task UploadAsync_CallsFileValidator_AndReturns400WhenRejected()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(db, validator: new RejectingValidator());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var result = await service.UploadAsync(new UploadDocumentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "doc.pdf", "application/pdf", 5, stream));

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("file_rejected", result.ErrorCode);
        Assert.Equal("Blocked by custom validator.", result.ErrorDetail);
    }

    [Fact]
    public async Task UploadAsync_ClientOfAnotherCase_Returns403()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(db, caseAccess: new FakeCaseAccess { Exists = true, CanAccess = false });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var result = await service.UploadAsync(new UploadDocumentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "doc.pdf", "application/pdf", 5, stream));

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_DuplicateItem_Returns409DocumentExists()
    {
        await using var db = CreateInMemoryDb();
        var storage = new FakeFileStorage();
        var service = CreateService(db, storage: storage);
        var caseId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        using var s1 = new MemoryStream(Encoding.UTF8.GetBytes("first"));
        var first = await service.UploadAsync(new UploadDocumentCommand(
            caseId, itemId, "first.pdf", "application/pdf", 5, s1));
        Assert.True(first.IsSuccess);

        using var s2 = new MemoryStream(Encoding.UTF8.GetBytes("second"));
        var second = await service.UploadAsync(new UploadDocumentCommand(
            caseId, itemId, "second.pdf", "application/pdf", 6, s2));

        Assert.False(second.IsSuccess);
        Assert.Equal(409, second.StatusCode);
        Assert.Equal("document_exists", second.ErrorCode);
        Assert.Single(storage.Blobs);
    }

    [Fact]
    public async Task UploadAsync_StorageFailure_LeavesNoDatabaseRows()
    {
        await using var db = CreateInMemoryDb();
        var storage = new FakeFileStorage { ThrowOnUpload = true };
        var service = CreateService(db, storage: storage);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(new UploadDocumentCommand(
                Guid.NewGuid(), Guid.NewGuid(), "doc.pdf", "application/pdf", 7, stream)));

        Assert.Empty(db.Documents);
        Assert.Empty(db.DocumentVersions);
    }

    [Fact]
    public async Task UploadAsync_DbOrAuditFailure_DeletesUploadedBlob()
    {
        await using var db = CreateInMemoryDb();
        var storage = new FakeFileStorage();
        var audit = new FakeAuditWriter { ThrowOnWrite = true };
        var service = CreateService(db, storage: storage, auditWriter: audit);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));

        var result = await service.UploadAsync(new UploadDocumentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "doc.pdf", "application/pdf", 7, stream));

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.StatusCode);
        Assert.Empty(storage.Blobs);
        Assert.Single(storage.DeletedBlobs);
    }

    [Fact]
    public async Task UploadAsync_ValidFile_StoresMetadataAndSha256AndAudit_AndListsWithoutStorageUrl()
    {
        await using var db = CreateInMemoryDb();
        var storage = new FakeFileStorage();
        var audit = new FakeAuditWriter();
        var user = new FakeCurrentUser();
        var service = CreateService(db, storage: storage, currentUser: user, auditWriter: audit);

        var payload = Encoding.UTF8.GetBytes("bank-statement-content");
        var expectedSha = Convert.ToHexStringLower(SHA256.HashData(payload));
        var caseId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        using var stream = new MemoryStream(payload);
        var upload = await service.UploadAsync(new UploadDocumentCommand(
            caseId, itemId, "../../bank-statement.pdf", "application/pdf", payload.Length, stream));

        Assert.True(upload.IsSuccess);
        Assert.Equal(201, upload.StatusCode);
        Assert.Equal(1, upload.Value!.VersionNumber);
        Assert.Equal("bank-statement.pdf", upload.Value.FileName);
        Assert.Equal(payload.Length, upload.Value.SizeBytes);

        var version = await db.DocumentVersions.SingleAsync();
        Assert.Equal(expectedSha, version.Sha256);
        Assert.StartsWith($"cases/{caseId}/{upload.Value.DocumentId}/v1/", version.BlobName);
        Assert.DoesNotContain("bank-statement", version.BlobName);

        Assert.Single(audit.Entries);
        Assert.Equal("DocumentUploaded", audit.Entries[0].Action);

        var list = await service.ListForCaseAsync(caseId);
        Assert.True(list.IsSuccess);
        var item = Assert.Single(list.Value!);
        Assert.Equal(upload.Value.DocumentId, item.DocumentId);
        Assert.Equal("bank-statement.pdf", item.FileName);
    }
}
