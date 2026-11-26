using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnboardFlow.Api.Modules.Onboarding.Domain;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Cases.Repositories;

public interface ICaseRepository
{
    Task<IReadOnlyList<OnboardingCase>> GetCasesAsync();
    Task<OnboardingCase?> GetCaseByIdAsync(Guid id);
    Task<IReadOnlyList<User>> GetCaseOwnersAsync();
}
