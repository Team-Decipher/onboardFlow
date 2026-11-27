using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OnboardFlow.Api.Modules.Documents.Application.Storage;

/// <summary>
/// Abstraction over private blob storage for uploaded documents.
/// </summary>
public interface IFileStorage
{
    Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string blobName, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string blobName, CancellationToken ct = default);
}
