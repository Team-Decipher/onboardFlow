using Moq;
using OnboardFlow.Application.DTOs;
using OnboardFlow.Application.Services;
using OnboardFlow.Core.Entities;
using OnboardFlow.Core.Enums;
using OnboardFlow.Core.Interfaces;
using Xunit;

namespace OnboardFlow.Tests;

public class CaseServiceTests
{
    private readonly Mock<IRepository<Case>> _mockRepo;
    private readonly CaseService _caseService;

    public CaseServiceTests()
    {
        _mockRepo = new Mock<IRepository<Case>>();
        _caseService = new CaseService(_mockRepo.Object);
    }

    [Fact]
    public async Task CreateCaseAsync_EnterpriseTier_GeneratesMandatoryFinancialChecklist()
    {
        // Arrange
        var dto = new CreateCaseDto("Acme Corp", ClientType.Enterprise, Guid.NewGuid());

        // Act
        var result = await _caseService.CreateCaseAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("ONB-", result.CaseNumber);
        Assert.Contains(result.ChecklistItems, item => item.Title.Contains("Financial Statements"));
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Case>()), Times.Once);
    }

    [Fact]
    public async Task CreateCaseAsync_InitialProgress_IsZeroPercent()
    {
        // Arrange
        var dto = new CreateCaseDto("Beta LLC", ClientType.SME, Guid.NewGuid());

        // Act
        var result = await _caseService.CreateCaseAsync(dto);

        // Assert
        Assert.Equal(0, result.ProgressPercentage);
    }
}
