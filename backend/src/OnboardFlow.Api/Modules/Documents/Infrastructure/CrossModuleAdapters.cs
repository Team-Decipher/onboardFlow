using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.Documents.Application.Abstractions;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Documents.Infrastructure;

public sealed class CaseAccessAdapter : ICaseAccess
{
    private readonly AppDbContext _appDb;

    public CaseAccessAdapter(AppDbContext appDb)
    {
        _appDb = appDb;
    }

    public Task<bool> CaseExistsAsync(Guid caseId, CancellationToken ct = default) =>
        _appDb.OnboardingCases.AsNoTracking().AnyAsync(c => c.Id == caseId, ct);

    public async Task<bool> CanClientAccessCaseAsync(Guid userId, Guid caseId, CancellationToken ct = default)
    {
        var user = await _appDb.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new { u.ClientOrganisationId })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return false;
        }

        return await _appDb.OnboardingCases
            .AsNoTracking()
            .AnyAsync(c => c.Id == caseId &&
                           (c.OwnerUserId == userId ||
                            (user.ClientOrganisationId != null && c.ClientOrganisationId == user.ClientOrganisationId)),
                ct);
    }

    public Task<bool> ChecklistItemBelongsToCaseAsync(Guid caseId, Guid checklistItemId, CancellationToken ct = default) =>
        _appDb.OnboardingCaseItems
            .AsNoTracking()
            .AnyAsync(i => i.Id == checklistItemId && i.OnboardingCaseId == caseId, ct);
}

public sealed class ChecklistItemReaderAdapter : IChecklistItemReader
{
    private readonly AppDbContext _appDb;

    public ChecklistItemReaderAdapter(AppDbContext appDb)
    {
        _appDb = appDb;
    }

    public async Task<ChecklistItemInfo?> GetItemAsync(Guid itemId, CancellationToken ct = default)
    {
        return await _appDb.OnboardingCaseItems
            .AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => new ChecklistItemInfo(
                i.Id,
                i.DocumentName,
                i.IsRequired,
                i.DueDate,
                i.AssignedReviewerId))
            .FirstOrDefaultAsync(ct);
    }
}

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            var raw = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}

public sealed class AuditWriterAdapter : IAuditWriter
{
    private readonly AppDbContext _appDb;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditWriterAdapter(AppDbContext appDb, IHttpContextAccessor httpContextAccessor)
    {
        _appDb = appDb;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(Guid actorId, string action, string entityType, Guid entityId, CancellationToken ct = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var entry = new AuditEntry
        {
            Action = action,
            ActorId = actorId.ToString(),
            ActorEmail = user?.FindFirstValue(ClaimTypes.Email),
            ActorRole = user?.FindFirstValue(ClaimTypes.Role),
            TargetEntity = entityType,
            TargetId = entityId.ToString(),
            TimestampUtc = DateTime.UtcNow
        };

        _appDb.AuditLogs.Add(entry);
        await _appDb.SaveChangesAsync(ct);
    }
}
