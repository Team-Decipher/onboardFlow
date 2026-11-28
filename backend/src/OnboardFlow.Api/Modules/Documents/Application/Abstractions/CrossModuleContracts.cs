using System;
using System.Threading;
using System.Threading.Tasks;

namespace OnboardFlow.Api.Modules.Documents.Application.Abstractions;

public interface ICaseAccess
{
    Task<bool> CaseExistsAsync(Guid caseId, CancellationToken ct = default);
    Task<bool> CanClientAccessCaseAsync(Guid userId, Guid caseId, CancellationToken ct = default);
    Task<bool> ChecklistItemBelongsToCaseAsync(Guid caseId, Guid checklistItemId, CancellationToken ct = default);
}

public sealed record ChecklistItemInfo(
    Guid Id,
    string DocumentName,
    bool IsRequired,
    DateTime? DueDate,
    Guid? AssignedReviewerId);

public interface IChecklistItemReader
{
    Task<ChecklistItemInfo?> GetItemAsync(Guid itemId, CancellationToken ct = default);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsInRole(string role);
}

public interface IAuditWriter
{
    Task WriteAsync(Guid actorId, string action, string entityType, Guid entityId, CancellationToken ct = default);
}
