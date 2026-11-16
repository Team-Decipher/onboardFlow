using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.Checklists.Domain;

namespace OnboardFlow.Api.Modules.Checklists.Repositories;

public class ChecklistItemRepository : IChecklistItemRepository
{
    private readonly AppDbContext _context;

    public ChecklistItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ChecklistItem>> GetByTemplateIdAsync(Guid templateId)
    {
        return await _context.ChecklistItems
            .Include(i => i.AssignedReviewer)
            .Where(i => i.ChecklistTemplateId == templateId)
            .OrderBy(i => i.OrderIndex)
            .ThenBy(i => i.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<ChecklistItem?> GetByIdAsync(Guid id)
    {
        return await _context.ChecklistItems
            .Include(i => i.AssignedReviewer)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<int> GetMaxOrderIndexAsync(Guid templateId)
    {
        var items = await _context.ChecklistItems
            .Where(i => i.ChecklistTemplateId == templateId)
            .Select(i => i.OrderIndex)
            .ToListAsync();

        return items.Count != 0 ? items.Max() : 0;
    }

    public async Task<ChecklistItem> AddAsync(ChecklistItem item)
    {
        await _context.ChecklistItems.AddAsync(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(ChecklistItem item)
    {
        _context.ChecklistItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ChecklistItem item)
    {
        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

