using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.Checklists.Domain;
using OnboardFlow.Api.Modules.Checklists.DTOs;
using OnboardFlow.Api.Modules.Checklists.Repositories;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Checklists.Services;

public class ChecklistItemService : IChecklistItemService
{
    private readonly IChecklistItemRepository _repository;
    private readonly AppDbContext _context;

    public ChecklistItemService(IChecklistItemRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IReadOnlyList<ChecklistItemDto>> GetItemsByTemplateIdAsync(Guid templateId)
    {
        var items = await _repository.GetByTemplateIdAsync(templateId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<ChecklistTemplateDetailDto?> GetTemplateDetailAsync(Guid templateId)
    {
        var template = await _context.ChecklistTemplates
            .Include(t => t.ClientTypes)
            .Include(t => t.Items)
                .ThenInclude(i => i.AssignedReviewer)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
            return null;

        var clientType = template.ClientTypes.FirstOrDefault();

        var orderedItems = template.Items
            .OrderBy(i => i.OrderIndex)
            .ThenBy(i => i.CreatedAtUtc)
            .Select(MapToDto)
            .ToList();

        return new ChecklistTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Version = template.Version,
            Description = template.Description,
            AssociatedClientTypeId = clientType?.Id,
            AssociatedClientTypeName = clientType?.Name,
            RequiredCount = orderedItems.Count(i => i.IsRequired),
            OptionalCount = orderedItems.Count(i => !i.IsRequired),
            Items = orderedItems
        };
    }

    public async Task<ChecklistItemDto> AddItemAsync(Guid templateId, CreateChecklistItemDto dto, string createdBy)
    {
        var template = await _context.ChecklistTemplates.FindAsync(templateId);
        if (template == null)
        {
            throw new KeyNotFoundException($"Checklist template with ID '{templateId}' was not found.");
        }

        // Determine next sequential OrderIndex
        int nextOrderIndex = dto.OrderIndex ?? (await _repository.GetMaxOrderIndexAsync(templateId) + 1);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = templateId,
            DocumentName = dto.DocumentName.Trim(),
            IsRequired = dto.IsRequired,
            AssignedReviewerId = dto.AssignedReviewerId,
            DueDays = dto.DueDays,
            DueDate = dto.DueDate,
            OrderIndex = nextOrderIndex,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "System administrator" : createdBy
        };

        await _repository.AddAsync(item);

        // Update counts on template
        await RecalculateTemplateCountsAsync(templateId);

        // Fetch reloaded item with reviewer details
        var reloaded = await _repository.GetByIdAsync(item.Id);
        return MapToDto(reloaded ?? item);
    }

    public async Task<bool> RemoveItemAsync(Guid templateId, Guid itemId)
    {
        var item = await _repository.GetByIdAsync(itemId);
        if (item == null || item.ChecklistTemplateId != templateId)
        {
            return false;
        }

        await _repository.DeleteAsync(item);

        // Re-compact and sequentially reorder remaining items
        var remainingItems = (await _repository.GetByTemplateIdAsync(templateId))
            .OrderBy(i => i.OrderIndex)
            .ThenBy(i => i.CreatedAtUtc)
            .ToList();

        for (int i = 0; i < remainingItems.Count; i++)
        {
            remainingItems[i].OrderIndex = i + 1;
        }
        await _repository.SaveChangesAsync();

        // Update counts on template
        await RecalculateTemplateCountsAsync(templateId);

        return true;
    }

    public async Task<IReadOnlyList<ChecklistItemDto>> BulkSaveItemsAsync(
        Guid templateId, 
        BulkUpdateChecklistItemsDto dto,
        string modifiedBy)
    {
        var template = await _context.ChecklistTemplates
            .Include(t => t.ClientTypes)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new KeyNotFoundException($"Checklist template with ID '{templateId}' was not found.");
        }

        // Update template header if provided
        if (!string.IsNullOrWhiteSpace(dto.ChecklistName))
        {
            template.Name = dto.ChecklistName.Trim();
        }

        // Update associated client type if provided
        if (dto.AssociatedClientTypeId.HasValue)
        {
            var targetClientType = await _context.ClientTypes.FindAsync(dto.AssociatedClientTypeId.Value);
            if (targetClientType != null)
            {
                // Clear previous client type mappings to this template if any
                var existingTypes = await _context.ClientTypes
                    .Where(c => c.DefaultChecklistTemplateId == templateId)
                    .ToListAsync();

                foreach (var ct in existingTypes)
                {
                    if (ct.Id != targetClientType.Id)
                        ct.DefaultChecklistTemplateId = null;
                }

                targetClientType.DefaultChecklistTemplateId = templateId;
            }
        }

        var existingItems = await _context.ChecklistItems
            .Where(i => i.ChecklistTemplateId == templateId)
            .ToListAsync();

        var existingItemMap = existingItems.ToDictionary(i => i.Id);
        var updatedItemIds = new HashSet<Guid>();

        for (int index = 0; index < dto.Items.Count; index++)
        {
            var itemDto = dto.Items[index];
            int orderIndex = index + 1;

            if (itemDto.Id.HasValue && existingItemMap.TryGetValue(itemDto.Id.Value, out var existingItem))
            {
                existingItem.DocumentName = itemDto.DocumentName.Trim();
                existingItem.IsRequired = itemDto.IsRequired;
                existingItem.AssignedReviewerId = itemDto.AssignedReviewerId;
                existingItem.DueDays = itemDto.DueDays;
                existingItem.DueDate = itemDto.DueDate;
                existingItem.OrderIndex = orderIndex;
                updatedItemIds.Add(existingItem.Id);
            }
            else
            {
                var newItem = new ChecklistItem
                {
                    Id = Guid.NewGuid(),
                    ChecklistTemplateId = templateId,
                    DocumentName = itemDto.DocumentName.Trim(),
                    IsRequired = itemDto.IsRequired,
                    AssignedReviewerId = itemDto.AssignedReviewerId,
                    DueDays = itemDto.DueDays,
                    DueDate = itemDto.DueDate,
                    OrderIndex = orderIndex,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = modifiedBy
                };
                await _context.ChecklistItems.AddAsync(newItem);
                updatedItemIds.Add(newItem.Id);
            }
        }

        // Remove any items that were deleted from the list
        var itemsToRemove = existingItems.Where(i => !updatedItemIds.Contains(i.Id)).ToList();
        if (itemsToRemove.Count != 0)
        {
            _context.ChecklistItems.RemoveRange(itemsToRemove);
        }

        await _context.SaveChangesAsync();

        // Recalculate template summary counts
        await RecalculateTemplateCountsAsync(templateId);

        var refreshed = await _repository.GetByTemplateIdAsync(templateId);
        return refreshed.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ReviewerOptionDto>> GetReviewersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive && 
                (u.Role == AppRoles.Reviewer || 
                 u.Role == AppRoles.Administrator || 
                 u.Role == AppRoles.OnboardingManager))
            .OrderBy(u => u.FullName)
            .Select(u => new ReviewerOptionDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Role = u.Role,
                Email = u.Email
            })
            .ToListAsync();
    }

    public async Task<ChecklistItemDto> AssignReviewerAsync(Guid templateId, Guid itemId, Guid? reviewerId, string modifiedBy)
    {
        var template = await _context.ChecklistTemplates.FindAsync(templateId);
        if (template == null)
        {
            throw new KeyNotFoundException($"Checklist template with ID '{templateId}' was not found.");
        }

        var item = await _context.ChecklistItems
            .Include(i => i.AssignedReviewer)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ChecklistTemplateId == templateId);

        if (item == null)
        {
            throw new KeyNotFoundException($"Checklist item with ID '{itemId}' not found on template '{templateId}'.");
        }

        User? newReviewer = null;
        if (reviewerId.HasValue)
        {
            newReviewer = await _context.Users.FirstOrDefaultAsync(u => u.Id == reviewerId.Value);
            if (newReviewer == null || !newReviewer.IsActive)
            {
                throw new ArgumentException("The specified reviewer user does not exist or is inactive.");
            }

            if (newReviewer.Role != AppRoles.Reviewer)
            {
                throw new ArgumentException($"User '{newReviewer.FullName}' cannot be assigned as a reviewer because they do not hold the '{AppRoles.Reviewer}' role.");
            }
        }

        var oldReviewerName = item.AssignedReviewer?.FullName ?? "None";
        var newReviewerName = newReviewer?.FullName ?? "None";

        item.AssignedReviewerId = reviewerId;
        item.AssignedReviewer = newReviewer;

        // Audit entry
        var audit = new AuditEntry
        {
            Id = Guid.NewGuid(),
            Action = "ChecklistItemReviewerAssigned",
            Details = $"Assigned reviewer '{newReviewerName}' to item '{item.DocumentName}' on template '{template.Name}' (previously: '{oldReviewerName}').",
            TimestampUtc = DateTime.UtcNow,
            User = string.IsNullOrWhiteSpace(modifiedBy) ? "System administrator" : modifiedBy
        };
        _context.AuditLogs.Add(audit);

        await _context.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<ChecklistItemDto> SetItemDueLimitAsync(
        Guid templateId, 
        Guid itemId, 
        SetItemDueLimitDto dto, 
        string modifiedBy, 
        TimeProvider? timeProvider = null)
    {
        var template = await _context.ChecklistTemplates.FindAsync(templateId);
        if (template == null)
        {
            throw new KeyNotFoundException($"Checklist template with ID '{templateId}' was not found.");
        }

        var item = await _context.ChecklistItems
            .Include(i => i.AssignedReviewer)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ChecklistTemplateId == templateId);

        if (item == null)
        {
            throw new KeyNotFoundException($"Checklist item with ID '{itemId}' not found on template '{templateId}'.");
        }

        var clock = timeProvider ?? TimeProvider.System;
        var todayUtc = clock.GetUtcNow().UtcDateTime.Date;

        if (dto.DueDays.HasValue && (dto.DueDays.Value < 1 || dto.DueDays.Value > 365))
        {
            throw new ArgumentException("Due days must be between 1 and 365 days.");
        }

        if (dto.DueDate.HasValue && dto.DueDate.Value.Date < todayUtc)
        {
            throw new ArgumentException("Due date cannot be in the past.");
        }

        item.DueDays = dto.DueDays;
        item.DueDate = dto.DueDate;

        var dueDaysDesc = dto.DueDays.HasValue ? $"{dto.DueDays.Value} days" : "None";
        var dueDateDesc = dto.DueDate.HasValue ? dto.DueDate.Value.ToString("yyyy-MM-dd") : "None";

        var audit = new AuditEntry
        {
            Id = Guid.NewGuid(),
            Action = "ChecklistItemDueUpdated",
            Details = $"Updated due limits for item '{item.DocumentName}' on template '{template.Name}': DueDays={dueDaysDesc}, DueDate={dueDateDesc}.",
            TimestampUtc = DateTime.UtcNow,
            User = string.IsNullOrWhiteSpace(modifiedBy) ? "System administrator" : modifiedBy
        };
        _context.AuditLogs.Add(audit);

        await _context.SaveChangesAsync();

        return MapToDto(item, clock);
    }

    private async Task RecalculateTemplateCountsAsync(Guid templateId)
    {
        var template = await _context.ChecklistTemplates.FindAsync(templateId);
        if (template != null)
        {
            var counts = await _context.ChecklistItems
                .Where(i => i.ChecklistTemplateId == templateId)
                .GroupBy(i => i.IsRequired)
                .Select(g => new { IsRequired = g.Key, Count = g.Count() })
                .ToListAsync();

            template.RequiredCount = counts.FirstOrDefault(c => c.IsRequired)?.Count ?? 0;
            template.OptionalCount = counts.FirstOrDefault(c => !c.IsRequired)?.Count ?? 0;
            await _context.SaveChangesAsync();
        }
    }

    private static ChecklistItemDto MapToDto(ChecklistItem item) => MapToDto(item, null);

    private static ChecklistItemDto MapToDto(ChecklistItem item, TimeProvider? timeProvider)
    {
        var clock = timeProvider ?? TimeProvider.System;
        var todayUtc = clock.GetUtcNow().UtcDateTime.Date;
        var isOverdue = item.DueDate.HasValue && item.DueDate.Value.Date < todayUtc;

        return new ChecklistItemDto
        {
            Id = item.Id,
            ChecklistTemplateId = item.ChecklistTemplateId,
            DocumentName = item.DocumentName,
            IsRequired = item.IsRequired,
            AssignedReviewerId = item.AssignedReviewerId,
            AssignedReviewerName = item.AssignedReviewer?.FullName,
            DueDays = item.DueDays,
            DueDate = item.DueDate,
            IsOverdue = isOverdue,
            OrderIndex = item.OrderIndex,
            CreatedAtUtc = item.CreatedAtUtc
        };
    }
}
