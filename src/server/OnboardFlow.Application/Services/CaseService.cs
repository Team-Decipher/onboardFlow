using OnboardFlow.Application.DTOs;
using OnboardFlow.Core.Entities;
using OnboardFlow.Core.Enums;
using OnboardFlow.Core.Interfaces;

namespace OnboardFlow.Application.Services;

public interface ICaseService
{
    Task<CaseResponseDto> CreateCaseAsync(CreateCaseDto dto);
    Task<CaseResponseDto?> GetCaseByIdAsync(Guid id);
    Task<IEnumerable<CaseResponseDto>> GetAllCasesAsync();
}

public class CaseService : ICaseService
{
    private readonly IRepository<Case> _caseRepo;

    public CaseService(IRepository<Case> caseRepo)
    {
        _caseRepo = caseRepo;
    }

    public async Task<CaseResponseDto> CreateCaseAsync(CreateCaseDto dto)
    {
        var newCase = new Case
        {
            CaseNumber = $"ONB-{DateTime.UtcNow.Year}-{new Random().Next(1000, 9999)}",
            ClientName = dto.ClientName,
            ClientType = dto.ClientType,
            OwnerId = dto.OwnerId,
            Status = CaseStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
        };

        // Add standard checklist templates based on client tier
        newCase.ChecklistItems.Add(new ChecklistItem
        {
            Title = "Certificate of Incorporation / Business Registration",
            Description = "Certified copy of certificate of incorporation from national registrar.",
            IsMandatory = true,
            CurrentStage = ReviewStage.Legal
        });

        newCase.ChecklistItems.Add(new ChecklistItem
        {
            Title = "Director & UBO Proof of Identity",
            Description = "Government issued ID or Passport copies for all directors holding >25%.",
            IsMandatory = true,
            CurrentStage = ReviewStage.Legal
        });

        if (dto.ClientType == ClientType.Enterprise)
        {
            newCase.ChecklistItems.Add(new ChecklistItem
            {
                Title = "Audited Financial Statements (Last 2 Years)",
                Description = "Full financial statements audited by accredited public accountant.",
                IsMandatory = true,
                CurrentStage = ReviewStage.Finance
            });
        }

        await _caseRepo.AddAsync(newCase);
        return MapToDto(newCase);
    }

    public async Task<CaseResponseDto?> GetCaseByIdAsync(Guid id)
    {
        var c = await _caseRepo.GetByIdAsync(id);
        return c == null ? null : MapToDto(c);
    }

    public async Task<IEnumerable<CaseResponseDto>> GetAllCasesAsync()
    {
        var cases = await _caseRepo.GetAllAsync();
        return cases.Select(MapToDto);
    }

    private static CaseResponseDto MapToDto(Case c)
    {
        var total = c.ChecklistItems.Count(i => i.IsMandatory);
        var completed = c.ChecklistItems.Count(i => i.IsMandatory && i.Status == DocumentStatus.Approved);
        var progress = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

        return new CaseResponseDto(
            c.Id,
            c.CaseNumber,
            c.ClientName,
            c.ClientType,
            c.Status,
            progress,
            c.CreatedAt,
            c.ChecklistItems.Select(i => new ChecklistItemDto(
                i.Id,
                i.Title,
                i.Description,
                i.IsMandatory,
                i.Status,
                i.CurrentStage,
                i.DueDate
            )).ToList()
        );
    }
}
