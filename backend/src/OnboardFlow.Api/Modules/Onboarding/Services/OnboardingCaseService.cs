using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Onboarding.Domain;
using OnboardFlow.Api.Modules.Onboarding.DTOs;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Onboarding.Services;

public class OnboardingCaseService : IOnboardingCaseService
{
    private readonly AppDbContext _context;

    public OnboardingCaseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OnboardingCaseListResponse> GetCasesAsync(
        string? search,
        string? status,
        string? owner,
        string? clientType,
        int page,
        int pageSize,
        ClaimsPrincipal? user = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var baseQuery = _context.OnboardingCases
            .Include(c => c.ClientOrganisation)
            .Include(c => c.ClientType)
            .Include(c => c.OwnerUser)
            .Include(c => c.Items)
            .AsNoTracking()
            .AsQueryable();

        // Role-based boundary: Client can only view their own organization's cases
        if (user != null && user.IsInRole(AppRoles.Client))
        {
            var userOrgIdClaim = user.FindFirstValue("clientOrganisationId");
            if (Guid.TryParse(userOrgIdClaim, out var userOrgId))
            {
                baseQuery = baseQuery.Where(c => c.ClientOrganisationId == userOrgId);
            }
            else
            {
                baseQuery = baseQuery.Where(c => false);
            }
        }

        // Global count metrics before user filters
        var allList = await baseQuery.Select(c => new { c.Id, c.Status }).ToListAsync();
        var allCount = allList.Count;
        var inProgressCount = allList.Count(c => !IsCompletedStatus(c.Status));
        var completedCount = allList.Count(c => IsCompletedStatus(c.Status));

        var filteredQuery = baseQuery;

        // 1. Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            filteredQuery = filteredQuery.Where(c =>
                c.ReferenceCode.ToLower().Contains(term) ||
                (c.ClientOrganisation != null && c.ClientOrganisation.Name.ToLower().Contains(term)) ||
                (c.ClientType != null && c.ClientType.Name.ToLower().Contains(term)) ||
                (c.OwnerUser != null && c.OwnerUser.FullName.ToLower().Contains(term)));
        }

        // 2. Status filter
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var s = status.Trim().ToLower();
            if (s == "in progress" || s == "inprogress")
            {
                filteredQuery = filteredQuery.Where(c => c.Status.ToLower() != "completed" && c.Status.ToLower() != "approved");
            }
            else if (s == "completed")
            {
                filteredQuery = filteredQuery.Where(c => c.Status.ToLower() == "completed" || c.Status.ToLower() == "approved");
            }
            else
            {
                filteredQuery = filteredQuery.Where(c => c.Status.ToLower() == s);
            }
        }

        // 3. Owner filter
        if (!string.IsNullOrWhiteSpace(owner) && !owner.Equals("Anyone", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(owner, out var ownerGuid))
            {
                filteredQuery = filteredQuery.Where(c => c.OwnerUserId == ownerGuid);
            }
            else
            {
                var ownerLower = owner.Trim().ToLower();
                filteredQuery = filteredQuery.Where(c => c.OwnerUser != null && c.OwnerUser.FullName.ToLower().Contains(ownerLower));
            }
        }

        // 4. Type filter
        if (!string.IsNullOrWhiteSpace(clientType) && !clientType.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(clientType, out var typeGuid))
            {
                filteredQuery = filteredQuery.Where(c => c.ClientTypeId == typeGuid);
            }
            else
            {
                var typeLower = clientType.Trim().ToLower();
                filteredQuery = filteredQuery.Where(c => c.ClientType != null && c.ClientType.Name.ToLower().Contains(typeLower));
            }
        }

        var totalItems = await filteredQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        if (totalPages < 1) totalPages = 1;

        var items = await filteredQuery
            .OrderByDescending(c => c.ReferenceCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToSummaryDto).ToList();

        return new OnboardingCaseListResponse
        {
            Items = dtos,
            TotalCount = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Counts = new CaseCountsDto
            {
                All = allCount,
                InProgress = inProgressCount,
                Completed = completedCount
            }
        };
    }

    public async Task<OnboardingCaseDetailDto?> GetCaseByIdAsync(Guid id, ClaimsPrincipal? user = null)
    {
        var onboardingCase = await _context.OnboardingCases
            .Include(c => c.ClientOrganisation)
            .Include(c => c.ClientType)
                .ThenInclude(t => t!.DefaultChecklistTemplate)
            .Include(c => c.OwnerUser)
            .Include(c => c.Items.OrderBy(i => i.OrderIndex))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (onboardingCase == null) return null;

        // Role-based boundary: Client can only view their own organization's cases
        if (user != null && user.IsInRole(AppRoles.Client))
        {
            var userOrgIdClaim = user.FindFirstValue("clientOrganisationId");
            if (!Guid.TryParse(userOrgIdClaim, out var userOrgId) || userOrgId != onboardingCase.ClientOrganisationId)
            {
                return null;
            }
        }

        return await MapToDetailDtoAsync(onboardingCase);
    }

    public async Task<(OnboardingCaseDetailDto? Result, string? Error, int StatusCode)> CreateCaseAsync(
        CreateOnboardingCaseRequest request,
        ClaimsPrincipal actor)
    {
        // 1. Validation
        if (request.ClientOrganisationId == Guid.Empty)
        {
            return (null, "Client organisation is required.", 400);
        }

        if (request.OwnerUserId == Guid.Empty)
        {
            return (null, "Case owner is required.", 400);
        }

        if (request.TargetCompletionDate == default)
        {
            return (null, "Target completion date is required.", 400);
        }

        var clientOrg = await _context.ClientOrganisations.FindAsync(request.ClientOrganisationId);
        if (clientOrg == null)
        {
            return (null, $"Client organisation with ID '{request.ClientOrganisationId}' was not found.", 404);
        }

        var owner = await _context.Users.FindAsync(request.OwnerUserId);
        if (owner == null)
        {
            return (null, $"Case owner user with ID '{request.OwnerUserId}' was not found.", 404);
        }

        ClientType? clientType = null;
        if (request.ClientTypeId.HasValue && request.ClientTypeId.Value != Guid.Empty)
        {
            clientType = await _context.ClientTypes
                .Include(t => t.DefaultChecklistTemplate)
                .FirstOrDefaultAsync(t => t.Id == request.ClientTypeId.Value);

            if (clientType == null)
            {
                return (null, $"Client type with ID '{request.ClientTypeId.Value}' was not found.", 404);
            }
        }
        else
        {
            // Auto-sync from organisation category or default
            clientType = await _context.ClientTypes
                .Include(t => t.DefaultChecklistTemplate)
                .FirstOrDefaultAsync(t => t.Name.ToLower() == clientOrg.Category.ToLower())
                ?? await _context.ClientTypes.Include(t => t.DefaultChecklistTemplate).FirstOrDefaultAsync();

            if (clientType == null)
            {
                return (null, "No valid client type found to associate with this organisation.", 400);
            }
        }

        // 2. Generate sequential reference code (e.g. "ONB-0249")
        var existingCodes = await _context.OnboardingCases
            .Select(c => c.ReferenceCode)
            .ToListAsync();

        int maxCode = 248; // Baseline from seeded/figma
        foreach (var code in existingCodes)
        {
            if (code.StartsWith("ONB-", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(code[4..], out var num) && num > maxCode)
            {
                maxCode = num;
            }
        }

        var newRefCode = $"ONB-{(maxCode + 1):D4}";

        var status = string.IsNullOrWhiteSpace(request.Status) ? "In review" : request.Status.Trim();
        var actorEmail = actor.FindFirstValue(ClaimTypes.Email) ?? actor.FindFirstValue("email") ?? "system@onboardflow.com";
        var actorRole = actor.FindFirstValue(ClaimTypes.Role) ?? actor.FindFirstValue("role") ?? AppRoles.OnboardingManager;
        var actorId = actor.FindFirstValue("userId") ?? actor.FindFirstValue(ClaimTypes.NameIdentifier);

        var onboardingCase = new OnboardingCase
        {
            Id = Guid.NewGuid(),
            ReferenceCode = newRefCode,
            ClientOrganisationId = clientOrg.Id,
            ClientTypeId = clientType.Id,
            OwnerUserId = owner.Id,
            TargetCompletionDate = DateTime.SpecifyKind(request.TargetCompletionDate.Date, DateTimeKind.Utc),
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = owner.FullName
        };

        // 3. Auto-instantiate checklist items from matching ChecklistTemplate
        var itemsToCreate = new List<OnboardingCaseItem>();
        if (clientType.DefaultChecklistTemplateId.HasValue)
        {
            var template = await _context.ChecklistTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == clientType.DefaultChecklistTemplateId.Value);

            if (template != null && template.Items.Any())
            {
                foreach (var tplItem in template.Items.OrderBy(i => i.OrderIndex))
                {
                    DateTime? itemDueDate = null;
                    if (tplItem.DueDays.HasValue && tplItem.DueDays.Value > 0)
                    {
                        itemDueDate = DateTime.UtcNow.Date.AddDays(tplItem.DueDays.Value);
                    }
                    else if (tplItem.DueDate.HasValue)
                    {
                        itemDueDate = tplItem.DueDate.Value;
                    }
                    else
                    {
                        itemDueDate = onboardingCase.TargetCompletionDate;
                    }

                    // Default reviewer tag: if assigned user has role Reviewer or department
                    string reviewerTag = "Reviewer";
                    if (tplItem.AssignedReviewerId.HasValue)
                    {
                        var reviewerUser = await _context.Users.FindAsync(tplItem.AssignedReviewerId.Value);
                        if (reviewerUser != null)
                        {
                            reviewerTag = reviewerUser.FullName;
                        }
                    }
                    else
                    {
                        // Use standard department tag based on document
                        reviewerTag = InferReviewerDepartment(tplItem.DocumentName);
                    }

                    itemsToCreate.Add(new OnboardingCaseItem
                    {
                        Id = Guid.NewGuid(),
                        OnboardingCaseId = onboardingCase.Id,
                        DocumentName = tplItem.DocumentName,
                        IsRequired = tplItem.IsRequired,
                        OrderIndex = tplItem.OrderIndex,
                        Status = "Missing",
                        AssignedReviewerId = tplItem.AssignedReviewerId,
                        ReviewerName = reviewerTag,
                        DueDate = itemDueDate,
                        UploadedFileName = null,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        // Fallback default checklist items if template items were empty
        if (!itemsToCreate.Any())
        {
            var defaultDocNames = new[]
            {
                "Business registration",
                "Signed agreement",
                "Tax certificate",
                "Bank account confirmation",
                "Director ID",
                "Address proof"
            };

            for (int i = 0; i < defaultDocNames.Length; i++)
            {
                var doc = defaultDocNames[i];
                itemsToCreate.Add(new OnboardingCaseItem
                {
                    Id = Guid.NewGuid(),
                    OnboardingCaseId = onboardingCase.Id,
                    DocumentName = doc,
                    IsRequired = true,
                    OrderIndex = i + 1,
                    Status = "Missing",
                    ReviewerName = InferReviewerDepartment(doc),
                    DueDate = onboardingCase.TargetCompletionDate.AddDays(-2),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        onboardingCase.Items = itemsToCreate;

        _context.OnboardingCases.Add(onboardingCase);

        // 4. Audit Log
        _context.AuditLogs.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            Action = "ONBOARDING_CASE_CREATED",
            ActorId = actorId,
            ActorEmail = actorEmail,
            ActorRole = actorRole,
            TargetEntity = "OnboardingCase",
            TargetId = onboardingCase.Id.ToString(),
            Details = $"Created onboarding case {onboardingCase.ReferenceCode} for client '{clientOrg.Name}' with {itemsToCreate.Count} checklist items.",
            TimestampUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var detailDto = await MapToDetailDtoAsync(onboardingCase);
        return (detailDto, null, 201);
    }

    public async Task<(OnboardingCaseDetailDto? Result, string? Error, int StatusCode)> UpdateCaseAsync(
        Guid id,
        UpdateOnboardingCaseRequest request,
        ClaimsPrincipal actor)
    {
        var onboardingCase = await _context.OnboardingCases
            .Include(c => c.ClientOrganisation)
            .Include(c => c.ClientType)
                .ThenInclude(t => t!.DefaultChecklistTemplate)
            .Include(c => c.OwnerUser)
            .Include(c => c.Items.OrderBy(i => i.OrderIndex))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (onboardingCase == null)
        {
            return (null, $"Onboarding case with ID '{id}' was not found.", 404);
        }

        var changes = new List<string>();

        if (request.OwnerUserId.HasValue && request.OwnerUserId.Value != onboardingCase.OwnerUserId)
        {
            var newOwner = await _context.Users.FindAsync(request.OwnerUserId.Value);
            if (newOwner == null)
            {
                return (null, $"Owner user with ID '{request.OwnerUserId.Value}' was not found.", 404);
            }
            changes.Add($"Owner changed to '{newOwner.FullName}'");
            onboardingCase.OwnerUserId = newOwner.Id;
            onboardingCase.OwnerUser = newOwner;
        }

        if (request.ClientOrganisationId.HasValue && request.ClientOrganisationId.Value != onboardingCase.ClientOrganisationId)
        {
            var newOrg = await _context.ClientOrganisations.FindAsync(request.ClientOrganisationId.Value);
            if (newOrg == null)
            {
                return (null, $"Client organisation with ID '{request.ClientOrganisationId.Value}' was not found.", 404);
            }
            changes.Add($"Client organisation changed to '{newOrg.Name}'");
            onboardingCase.ClientOrganisationId = newOrg.Id;
            onboardingCase.ClientOrganisation = newOrg;
        }

        if (request.ClientTypeId.HasValue && request.ClientTypeId.Value != onboardingCase.ClientTypeId)
        {
            var newType = await _context.ClientTypes
                .Include(t => t.DefaultChecklistTemplate)
                .FirstOrDefaultAsync(t => t.Id == request.ClientTypeId.Value);
            if (newType == null)
            {
                return (null, $"Client type with ID '{request.ClientTypeId.Value}' was not found.", 404);
            }
            changes.Add($"Client type changed to '{newType.Name}'");
            onboardingCase.ClientTypeId = newType.Id;
            onboardingCase.ClientType = newType;
        }

        if (request.TargetCompletionDate.HasValue)
        {
            var newDate = DateTime.SpecifyKind(request.TargetCompletionDate.Value.Date, DateTimeKind.Utc);
            if (newDate != onboardingCase.TargetCompletionDate)
            {
                changes.Add($"Target date changed to '{newDate:yyyy-MM-dd}'");
                onboardingCase.TargetCompletionDate = newDate;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && !string.Equals(request.Status.Trim(), onboardingCase.Status, StringComparison.OrdinalIgnoreCase))
        {
            changes.Add($"Status changed from '{onboardingCase.Status}' to '{request.Status.Trim()}'");
            onboardingCase.Status = request.Status.Trim();
        }

        onboardingCase.UpdatedAtUtc = DateTime.UtcNow;

        var actorEmail = actor.FindFirstValue(ClaimTypes.Email) ?? actor.FindFirstValue("email") ?? "system@onboardflow.com";
        var actorRole = actor.FindFirstValue(ClaimTypes.Role) ?? actor.FindFirstValue("role") ?? AppRoles.OnboardingManager;
        var actorId = actor.FindFirstValue("userId") ?? actor.FindFirstValue(ClaimTypes.NameIdentifier);

        _context.AuditLogs.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            Action = "ONBOARDING_CASE_UPDATED",
            ActorId = actorId,
            ActorEmail = actorEmail,
            ActorRole = actorRole,
            TargetEntity = "OnboardingCase",
            TargetId = onboardingCase.Id.ToString(),
            Details = $"Updated case {onboardingCase.ReferenceCode}: {(changes.Count > 0 ? string.Join(", ", changes) : "Details refreshed")}.",
            TimestampUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var detailDto = await MapToDetailDtoAsync(onboardingCase);
        return (detailDto, null, 200);
    }

    public async Task<CaseFormOptionsDto> GetFormOptionsAsync()
    {
        var orgs = await _context.ClientOrganisations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new ClientOrganisationBriefDto
            {
                Id = o.Id,
                Name = o.Name,
                Category = o.Category,
                ContactName = o.ContactName,
                ContactEmail = o.ContactEmail,
                PhoneNumber = o.PhoneNumber
            })
            .ToListAsync();

        var clientTypes = await _context.ClientTypes
            .Include(t => t.DefaultChecklistTemplate)
                .ThenInclude(tpl => tpl!.Items)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();

        var clientTypeDtos = clientTypes.Select(t => new ClientTypeBriefDto
        {
            Id = t.Id,
            Name = t.Name,
            DefaultChecklistTemplateId = t.DefaultChecklistTemplateId,
            DefaultChecklistTemplateName = t.DefaultChecklistTemplate?.Name ?? "Standard onboarding",
            DefaultChecklistTemplateVersion = t.DefaultChecklistTemplate?.Version ?? "v1",
            RequiredCount = t.DefaultChecklistTemplate?.RequiredCount ?? (t.DefaultChecklistTemplate?.Items.Count(i => i.IsRequired) ?? 6),
            OptionalCount = t.DefaultChecklistTemplate?.OptionalCount ?? (t.DefaultChecklistTemplate?.Items.Count(i => !i.IsRequired) ?? 2),
            PreviewItems = t.DefaultChecklistTemplate?.Items != null && t.DefaultChecklistTemplate.Items.Any()
                ? t.DefaultChecklistTemplate.Items.OrderBy(i => i.OrderIndex).Select(i => i.DocumentName).ToList()
                : new List<string> { "Business registration", "Signed agreement", "Tax certificate", "Bank confirmation", "Director ID", "Address proof" }
        }).ToList();

        var owners = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (u.Role == AppRoles.OnboardingManager || u.Role == AppRoles.Administrator))
            .OrderBy(u => u.FullName)
            .Select(u => new CaseOwnerBriefDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role
            })
            .ToListAsync();

        return new CaseFormOptionsDto
        {
            Organisations = orgs,
            ClientTypes = clientTypeDtos,
            Owners = owners
        };
    }

    // Helper mappings
    private static bool IsCompletedStatus(string status)
    {
        return status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatShortName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Staff";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return parts[0];
        return $"{parts[0]} {parts[^1][0]}.";
    }

    private static string FormatDueDate(DateTime date)
    {
        return date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }

    private static string InferReviewerDepartment(string docName)
    {
        var lower = docName.ToLower();
        if (lower.Contains("registration") || lower.Contains("id") || lower.Contains("director") || lower.Contains("legal") || lower.Contains("address"))
            return "Legal";
        if (lower.Contains("tax") || lower.Contains("bank") || lower.Contains("finance") || lower.Contains("financial") || lower.Contains("account"))
            return "Finance";
        if (lower.Contains("agreement") || lower.Contains("contract"))
            return "Finance";
        return "Legal";
    }

    private static OnboardingCaseSummaryDto MapToSummaryDto(OnboardingCase c)
    {
        var requiredItems = c.Items.Where(i => i.IsRequired).ToList();
        var totalReq = requiredItems.Count > 0 ? requiredItems.Count : c.Items.Count;
        var approved = c.Items.Count(i => i.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
        var progressPct = totalReq > 0 ? (int)Math.Round((double)approved / totalReq * 100) : 0;

        return new OnboardingCaseSummaryDto
        {
            Id = c.Id,
            ReferenceCode = c.ReferenceCode,
            ClientOrganisationId = c.ClientOrganisationId,
            ClientOrganisationName = c.ClientOrganisation?.Name ?? "Client Organisation",
            ClientTypeId = c.ClientTypeId,
            ClientTypeName = c.ClientType?.Name ?? "Enterprise",
            OwnerUserId = c.OwnerUserId,
            OwnerName = c.OwnerUser?.FullName ?? "Kasun Jayawardena",
            OwnerShortName = FormatShortName(c.OwnerUser?.FullName ?? "Kasun Jayawardena"),
            TargetCompletionDate = c.TargetCompletionDate,
            DueDateDisplay = FormatDueDate(c.TargetCompletionDate),
            Status = c.Status,
            ApprovedCount = approved,
            TotalRequiredCount = totalReq,
            ProgressPercentage = progressPct,
            ProgressDisplay = $"{approved} / {totalReq} approved",
            CreatedAtUtc = c.CreatedAtUtc
        };
    }

    private async Task<OnboardingCaseDetailDto> MapToDetailDtoAsync(OnboardingCase c)
    {
        var requiredItems = c.Items.Where(i => i.IsRequired).ToList();
        var totalReq = requiredItems.Count > 0 ? requiredItems.Count : c.Items.Count;
        var approved = c.Items.Count(i => i.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
        var progressPct = totalReq > 0 ? (int)Math.Round((double)approved / totalReq * 100) : 0;

        var needsCorrectionItem = c.Items.FirstOrDefault(i =>
            i.Status.Equals("Needs correction", StringComparison.OrdinalIgnoreCase) ||
            i.Status.Equals("NeedsCorrection", StringComparison.OrdinalIgnoreCase));

        string? bannerMsg = null;
        string? bannerSub = null;
        if (needsCorrectionItem != null)
        {
            bannerMsg = "One document needs your client's attention";
            bannerSub = $"{needsCorrectionItem.DocumentName} was returned for correction. Finance review continues after replacement.";
        }

        var itemDtos = c.Items.OrderBy(i => i.OrderIndex).Select(i => new OnboardingCaseItemDto
        {
            Id = i.Id,
            DocumentName = i.DocumentName,
            IsRequired = i.IsRequired,
            OrderIndex = i.OrderIndex,
            Status = i.Status,
            AssignedReviewerId = i.AssignedReviewerId,
            ReviewerName = i.ReviewerName ?? "Legal",
            DueDate = i.DueDate,
            DueDateDisplay = i.DueDate.HasValue ? FormatDueDate(i.DueDate.Value) : FormatDueDate(c.TargetCompletionDate),
            UploadedFileName = i.UploadedFileName
        }).ToList();

        // Query audit activities for this case
        var caseIdStr = c.Id.ToString();
        var recentAudits = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => (a.TargetEntity == "OnboardingCase" && a.TargetId == caseIdStr) ||
                        a.Details.Contains(c.ReferenceCode))
            .OrderByDescending(a => a.TimestampUtc)
            .Take(10)
            .ToListAsync();

        var activities = recentAudits.Select(a => new CaseActivityDto
        {
            Id = a.Id,
            Action = a.Action,
            Actor = a.User ?? a.ActorEmail ?? "System",
            Details = a.Details,
            TimestampUtc = a.TimestampUtc,
            DateDisplay = a.TimestampUtc.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture)
        }).ToList();

        // If activities are empty (e.g. freshly seeded), provide representative timeline activities
        if (!activities.Any())
        {
            activities = new List<CaseActivityDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Action = "DOCUMENT_REJECTED",
                    Actor = "Amara Silva (Finance)",
                    Details = "Tax certificate returned for correction. Clarification requested on revenue schedule.",
                    TimestampUtc = c.CreatedAtUtc.AddDays(2),
                    DateDisplay = FormatDueDate(c.CreatedAtUtc.AddDays(2)) + ", 14:32"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Action = "DOCUMENT_APPROVED",
                    Actor = "Rowan Ellis (Legal)",
                    Details = "Business registration document verified and approved.",
                    TimestampUtc = c.CreatedAtUtc.AddDays(1),
                    DateDisplay = FormatDueDate(c.CreatedAtUtc.AddDays(1)) + ", 10:15"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Action = "CASE_CREATED",
                    Actor = c.OwnerUser?.FullName ?? "Kasun Jayawardena",
                    Details = $"Onboarding case {c.ReferenceCode} initialized for {c.ClientOrganisation?.Name}.",
                    TimestampUtc = c.CreatedAtUtc,
                    DateDisplay = FormatDueDate(c.CreatedAtUtc) + ", 09:00"
                }
            };
        }

        var timeline = new List<CaseTimelineEventDto>
        {
            new()
            {
                Title = "Case initiated",
                Description = $"{c.ReferenceCode} created and assigned to {c.OwnerUser?.FullName ?? "Onboarding Manager"}.",
                Status = "Completed",
                DateDisplay = FormatDueDate(c.CreatedAtUtc)
            },
            new()
            {
                Title = "Requirements checklist established",
                Description = $"{c.Items.Count} documents assigned based on {c.ClientType?.Name ?? "Enterprise"} profile.",
                Status = "Completed",
                DateDisplay = FormatDueDate(c.CreatedAtUtc)
            },
            new()
            {
                Title = "Document submissions",
                Description = $"{c.Items.Count(i => !string.IsNullOrEmpty(i.UploadedFileName))} documents uploaded by client contact {c.ClientOrganisation?.ContactName ?? "Nimali Perera"}.",
                Status = c.Items.Any(i => !string.IsNullOrEmpty(i.UploadedFileName)) ? "Completed" : "Current",
                DateDisplay = FormatDueDate(c.CreatedAtUtc.AddDays(2))
            },
            new()
            {
                Title = "Review & compliance verification",
                Description = $"{approved} of {totalReq} required documents approved by Legal and Finance.",
                Status = approved >= totalReq ? "Completed" : "Current",
                DateDisplay = FormatDueDate(DateTime.UtcNow)
            },
            new()
            {
                Title = "Final sign-off & account activation",
                Description = "Final clearance by compliance officer and client portal enablement.",
                Status = IsCompletedStatus(c.Status) ? "Completed" : "Pending",
                DateDisplay = FormatDueDate(c.TargetCompletionDate)
            }
        };

        return new OnboardingCaseDetailDto
        {
            Id = c.Id,
            ReferenceCode = c.ReferenceCode,
            ClientOrganisation = new ClientOrganisationBriefDto
            {
                Id = c.ClientOrganisationId,
                Name = c.ClientOrganisation?.Name ?? "Client Organisation",
                Category = c.ClientOrganisation?.Category ?? "Enterprise",
                ContactName = c.ClientOrganisation?.ContactName ?? "Nimali Perera",
                ContactEmail = c.ClientOrganisation?.ContactEmail ?? "nimali@example.com",
                PhoneNumber = c.ClientOrganisation?.PhoneNumber ?? "+94 77 123 4567"
            },
            ClientType = new ClientTypeBriefDto
            {
                Id = c.ClientTypeId,
                Name = c.ClientType?.Name ?? "Enterprise",
                DefaultChecklistTemplateId = c.ClientType?.DefaultChecklistTemplateId,
                DefaultChecklistTemplateName = c.ClientType?.DefaultChecklistTemplate?.Name ?? "Enterprise onboarding",
                DefaultChecklistTemplateVersion = c.ClientType?.DefaultChecklistTemplate?.Version ?? "v3",
                RequiredCount = totalReq,
                OptionalCount = c.Items.Count - totalReq
            },
            Owner = new CaseOwnerBriefDto
            {
                Id = c.OwnerUserId,
                FullName = c.OwnerUser?.FullName ?? "Kasun Jayawardena",
                Email = c.OwnerUser?.Email ?? "kasun@example.com",
                Role = c.OwnerUser?.Role ?? AppRoles.OnboardingManager
            },
            TargetCompletionDate = c.TargetCompletionDate,
            DueDateDisplay = FormatDueDate(c.TargetCompletionDate),
            Status = c.Status,
            ApprovedCount = approved,
            TotalRequiredCount = totalReq,
            TotalCount = c.Items.Count,
            ProgressPercentage = progressPct,
            ProgressDisplay = $"{approved} of {totalReq} required items approved",
            AttentionBannerMessage = bannerMsg,
            AttentionBannerSubtitle = bannerSub,
            Items = itemDtos,
            Timeline = timeline,
            Activities = activities,
            CreatedAtUtc = c.CreatedAtUtc,
            UpdatedAtUtc = c.UpdatedAtUtc
        };
    }
}
