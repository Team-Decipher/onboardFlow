using OnboardFlow.Core.Enums;

namespace OnboardFlow.Application.DTOs;

public record CreateCaseDto(string ClientName, ClientType ClientType, Guid OwnerId);

public record CaseResponseDto(
    Guid Id,
    string CaseNumber,
    string ClientName,
    ClientType ClientType,
    CaseStatus Status,
    double ProgressPercentage,
    DateTime CreatedAt,
    List<ChecklistItemDto> ChecklistItems
);

public record ChecklistItemDto(
    Guid Id,
    string Title,
    string Description,
    bool IsMandatory,
    DocumentStatus Status,
    ReviewStage CurrentStage,
    DateTime? DueDate
);

public record ReviewDocumentDto(
    bool Approved,
    string? Comments,
    ReviewStage? NextStage
);
