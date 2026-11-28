using System.IO;
using OnboardFlow.Api.Modules.Documents.Application.Validation;

namespace OnboardFlow.Api.Tests.Documents;

public class FileValidationAndSanitizerTests
{
    [Theory]
    [InlineData("../../x.pdf", "x.pdf")]
    [InlineData("/etc/passwd", "passwd")]
    [InlineData(@"C:\Users\client\Desktop\bank-statement.pdf", "bank-statement.pdf")]
    [InlineData("my\0file\nname\r\t.pdf", "myfilename.pdf")]
    [InlineData("   ", "document")]
    [InlineData(null, "document")]
    [InlineData("..", "document")]
    public void Sanitize_ExtractsLastSegmentAndStripsControlCharacters(string? input, string expected)
    {
        var result = FileNameSanitizer.Sanitize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_TruncatesVeryLongNamesTo255Characters_PreservingExtension()
    {
        var longBase = new string('a', 300);
        var input = $"{longBase}.pdf";

        var result = FileNameSanitizer.Sanitize(input);

        Assert.Equal(255, result.Length);
        Assert.EndsWith(".pdf", result);
    }

    [Fact]
    public void PassThroughFileValidator_AcceptsAnyFile()
    {
        var validator = new PassThroughFileValidator();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var info = new FileUploadInfo("test.bin", "application/octet-stream", 3, stream);

        var result = validator.Validate(info);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }
}
