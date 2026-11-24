using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Checklists.Domain;
using OnboardFlow.Api.Modules.Checklists.DTOs;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Checklists.Services;

public class ChecklistTemplateService : IChecklistTemplateService
{
    private readonly AppDbContext _context;

    public ChecklistTemplateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ChecklistTemplateDto>> GetTemplatesAsync(Guid? clientTypeId = null)
    {
        var query = _context.ChecklistTemplates
            .Include(t => t.ClientTypes)
            .Include(t => t.Items)
            .AsQueryable();

        if (clientTypeId.HasValue)
        {
            query = query.Where(t => t.ClientTypes.Any(ct => ct.Id == clientTypeId.Value));
        }

        var templates = await query
            .OrderBy(t => t.Name)
            .ToListAsync();

        return templates.Select(t =>
        {
            var ct = t.ClientTypes.FirstOrDefault();
            var reqCount = t.Items.Any() ? t.Items.Count(i => i.IsRequired) : t.RequiredCount;
            var optCount = t.Items.Any() ? t.Items.Count(i => !i.IsRequired) : t.OptionalCount;

            return new ChecklistTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Version = t.Version,
                Description = t.Description,
                AssociatedClientTypeId = ct?.Id,
                AssociatedClientTypeName = ct?.Name,
                RequiredCount = reqCount,
                OptionalCount = optCount,
                CreatedAtUtc = t.CreatedAtUtc
            };
        }).ToList();
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
            .Select(i => new ChecklistItemDto
            {
                Id = i.Id,
                ChecklistTemplateId = i.ChecklistTemplateId,
                DocumentName = i.DocumentName,
                IsRequired = i.IsRequired,
                AssignedReviewerId = i.AssignedReviewerId,
                AssignedReviewerName = i.AssignedReviewer?.FullName,
                DueDate = i.DueDate,
                OrderIndex = i.OrderIndex,
                CreatedAtUtc = i.CreatedAtUtc
            })
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

    public async Task<ChecklistTemplateDetailDto> CreateTemplateAsync(CreateChecklistTemplateDto dto, string createdBy)
    {
        var trimmedName = dto.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Length < 3 || trimmedName.Length > 150)
        {
            throw new ArgumentException("Template name must be between 3 and 150 characters.");
        }

        var clientType = await _context.ClientTypes
            .FirstOrDefaultAsync(ct => ct.Id == dto.ClientTypeId);

        if (clientType == null)
        {
            throw new KeyNotFoundException($"Client type with ID '{dto.ClientTypeId}' was not found.");
        }

        var duplicateExists = await _context.ChecklistTemplates
            .AnyAsync(t => t.ClientTypes.Any(ct => ct.Id == dto.ClientTypeId) &&
                           t.Name.ToLower() == trimmedName.ToLower());

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A checklist template with the name '{trimmedName}' already exists for client type '{clientType.Name}'.");
        }

        var template = new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Version = string.IsNullOrWhiteSpace(dto.Version) ? "v1" : dto.Version.Trim(),
            Description = dto.Description?.Trim(),
            RequiredCount = 0,
            OptionalCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "System administrator" : createdBy
        };

        _context.ChecklistTemplates.Add(template);

        // Associate with client type
        clientType.DefaultChecklistTemplateId = template.Id;
        template.ClientTypes.Add(clientType);

        // Audit entry
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            Action = "ChecklistTemplateCreated",
            Details = $"Created checklist template '{template.Name}' ({template.Version}) for client type '{clientType.Name}'.",
            TimestampUtc = DateTime.UtcNow,
            User = template.CreatedBy
        };
        _context.AuditLogs.Add(auditEntry);

        await _context.SaveChangesAsync();

        return new ChecklistTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Version = template.Version,
            Description = template.Description,
            AssociatedClientTypeId = clientType.Id,
            AssociatedClientTypeName = clientType.Name,
            RequiredCount = 0,
            OptionalCount = 0,
            Items = new List<ChecklistItemDto>()
        };
    }
}
