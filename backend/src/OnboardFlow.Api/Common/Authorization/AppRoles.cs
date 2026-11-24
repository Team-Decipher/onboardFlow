namespace OnboardFlow.Api.Common.Authorization;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string OnboardingManager = "OnboardingManager";
    public const string Reviewer = "Reviewer";
    public const string Client = "Client";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Administrator,
        OnboardingManager,
        Reviewer,
        Client
    };

    public static bool IsValidRole(string? role) =>
        !string.IsNullOrWhiteSpace(role) && All.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static bool RequiresClientOrganisation(string? role) =>
        string.Equals(role, Client, StringComparison.OrdinalIgnoreCase);
}
