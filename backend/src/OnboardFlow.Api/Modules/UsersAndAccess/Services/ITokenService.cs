using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Services;

public interface ITokenService
{
    string GenerateToken(UserDto user);
}