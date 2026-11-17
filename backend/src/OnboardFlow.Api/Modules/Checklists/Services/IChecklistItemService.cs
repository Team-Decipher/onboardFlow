using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnboardFlow.Api.Modules.Checklists.DTOs;

namespace OnboardFlow.Api.Modules.Checklists.Services;

public interface IChecklistItemService
{
    Task<IReadOnlyList<ChecklistItemDto>> GetItemsByTemplateIdAsync(Guid templateId);
    Task<ChecklistTemplateDetailDto?> GetTemplateDetailAsync(Guid templateId);
    Task<ChecklistItemDto> AddItemAsync(Guid templateId, CreateChecklistItemDto dto, string createdBy);
    Task<bool> RemoveItemAsync(Guid templateId, Guid itemId);
    Task<IReadOnlyList<ChecklistItemDto>> BulkSaveItemsAsync(Guid templateId, BulkUpdateChecklistItemsDto dto, string modifiedBy);
    Task<IReadOnlyList<ReviewerOptionDto>> GetReviewersAsync();
    Task<ChecklistItemDto> AssignReviewerAsync(Guid templateId, Guid itemId, Guid? reviewerId, string modifiedBy);
}

