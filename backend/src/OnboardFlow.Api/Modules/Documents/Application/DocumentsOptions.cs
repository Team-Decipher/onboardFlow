using System.ComponentModel.DataAnnotations;

namespace OnboardFlow.Api.Modules.Documents.Application;

/// <summary>
/// Configuration options for the Documents module.
/// </summary>
public sealed class DocumentsOptions
{
    public const string SectionName = "Documents";

    /// <summary>
    /// Hard request-size ceiling for the upload endpoint (default 50 MB).
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "Documents:HardRequestLimitBytes must be greater than 0.")]
    public long HardRequestLimitBytes { get; set; } = 52_428_800;
}

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureStorage:ContainerName is required.")]
    public string ContainerName { get; set; } = "documents";

    /// <summary>
    /// Provided via user-secrets, environment variables (AzureStorage__ConnectionString), or appsettings.Development.json.
    /// </summary>
    public string? ConnectionString { get; set; }
}
