using System;

namespace OnboardFlow.Api.Modules.Documents.Application.DTOs;

public sealed record UploadDocumentResponseDto(
    Guid DocumentId,
    int VersionNumber,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

public sealed record CaseDocumentListItemDto(
    Guid DocumentId,
    Guid ChecklistItemId,
    int CurrentVersionNumber,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt,
    Guid UploadedBy);

public sealed record DocumentOperationResult<T>(
    bool IsSuccess,
    int StatusCode,
    T? Value = default,
    string? ErrorCode = null,
    string? ErrorTitle = null,
    string? ErrorDetail = null)
{
    public static DocumentOperationResult<T> Ok(int statusCode, T value) =>
        new(true, statusCode, value);

    public static DocumentOperationResult<T> Fail(int statusCode, string? errorCode, string title, string detail) =>
        new(false, statusCode, default, errorCode, title, detail);
}
