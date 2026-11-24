using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Checklists.DTOs;

namespace OnboardFlow.Api.Modules.Checklists.Services;

public interface IChecklistTemplateService
{
    Task<IReadOnlyList<ChecklistTemplateDto>> GetTemplatesAsync(Guid? clientTypeId = null);
    Task<ChecklistTemplateDetailDto?> GetTemplateDetailAsync(Guid templateId);
    Task<ChecklistTemplateDetailDto> CreateTemplateAsync(CreateChecklistTemplateDto dto, string createdBy);
}
