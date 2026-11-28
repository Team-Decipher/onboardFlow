using System.IO;

namespace OnboardFlow.Api.Modules.Documents.Application.Validation;

/// <summary>
/// Metadata and stream for a file being validated before storage.
/// </summary>
public sealed record FileUploadInfo(
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

/// <summary>
/// Result of validating an uploaded file.
/// </summary>
public sealed record FileValidationResult(bool IsValid, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static FileValidationResult Success() => new(true);
    public static FileValidationResult Fail(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
}

/// <summary>
/// Extension point for document validation (US-21 type checks, US-22 size checks).
/// </summary>
public interface IFileValidator
{
    FileValidationResult Validate(FileUploadInfo file);
}

/// <summary>
/// Default US-20 validator that accepts all files once safety basics pass.
/// </summary>
public sealed class PassThroughFileValidator : IFileValidator
{
    public FileValidationResult Validate(FileUploadInfo file) => FileValidationResult.Success();
}
