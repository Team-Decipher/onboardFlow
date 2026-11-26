using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.Onboarding.Domain;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Cases.Repositories;

public class CaseRepository : ICaseRepository
{
    private readonly AppDbContext _context;

    public CaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OnboardingCase>> GetCasesAsync()
    {
        return await _context.OnboardingCases
            .Include(c => c.ClientOrganisation)
            .Include(c => c.OwnerUser)
            .Include(c => c.ClientType)
            .Include(c => c.Items)
            .OrderByDescending(c => c.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<OnboardingCase?> GetCaseByIdAsync(Guid id)
    {
        return await _context.OnboardingCases
            .Include(c => c.ClientOrganisation)
            .Include(c => c.OwnerUser)
            .Include(c => c.ClientType)
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IReadOnlyList<User>> GetCaseOwnersAsync()
    {
        var ownerIds = await _context.OnboardingCases
            .Select(c => c.OwnerUserId)
            .Distinct()
            .ToListAsync();

        return await _context.Users
            .Where(u => ownerIds.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync();
    }
}
