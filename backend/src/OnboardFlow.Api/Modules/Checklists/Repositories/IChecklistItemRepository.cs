using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnboardFlow.Api.Modules.Checklists.Domain;

namespace OnboardFlow.Api.Modules.Checklists.Repositories;

public interface IChecklistItemRepository
{
    Task<IReadOnlyList<ChecklistItem>> GetByTemplateIdAsync(Guid templateId);
    Task<ChecklistItem?> GetByIdAsync(Guid id);
    Task<int> GetMaxOrderIndexAsync(Guid templateId);
    Task<ChecklistItem> AddAsync(ChecklistItem item);
    Task UpdateAsync(ChecklistItem item);
    Task DeleteAsync(ChecklistItem item);
    Task SaveChangesAsync();
}

