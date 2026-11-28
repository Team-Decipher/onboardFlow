using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using OnboardFlow.Api.Modules.Documents.Application;
using OnboardFlow.Api.Modules.Documents.Application.Storage;

namespace OnboardFlow.Api.Modules.Documents.Infrastructure;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorage"/>.
/// Streams uploads directly without buffering the entire file in memory.
/// </summary>
public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobFileStorage(BlobServiceClient blobServiceClient, IOptions<AzureStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        ArgumentNullException.ThrowIfNull(options);

        _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
    }

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(content, uploadOptions, ct);
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }

    public async Task<Stream> OpenReadAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        return await blobClient.OpenReadAsync(cancellationToken: ct);
    }
}
