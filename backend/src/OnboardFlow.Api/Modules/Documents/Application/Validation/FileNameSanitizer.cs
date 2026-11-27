using System;
using System.IO;
using System.Text;

namespace OnboardFlow.Api.Modules.Documents.Application.Validation;

/// <summary>
/// Sanitizes user-supplied file names for safe display and metadata storage.
/// </summary>
public static class FileNameSanitizer
{
    public const int MaxLength = 255;

    /// <summary>
    /// Keeps only the last path segment, strips control characters, and caps length at 255 characters.
    /// </summary>
    public static string Sanitize(string? rawFileName)
    {
        if (string.IsNullOrWhiteSpace(rawFileName))
        {
            return "document";
        }

        // Normalize both Unix and Windows separators to extract only the last segment
        var normalized = rawFileName.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        var segment = lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;

        var sb = new StringBuilder(segment.Length);
        foreach (var ch in segment)
        {
            if (!char.IsControl(ch))
            {
                sb.Append(ch);
            }
        }

        var cleaned = sb.ToString().Trim();
        if (string.IsNullOrEmpty(cleaned) || cleaned is "." or "..")
        {
            return "document";
        }

        if (cleaned.Length <= MaxLength)
        {
            return cleaned;
        }

        var ext = Path.GetExtension(cleaned);
        if (!string.IsNullOrEmpty(ext) && ext.Length < MaxLength)
        {
            var baseLen = MaxLength - ext.Length;
            return cleaned[..baseLen] + ext;
        }

        return cleaned[..MaxLength];
    }
}
