using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using OnboardFlow.Core.Interfaces;

namespace OnboardFlow.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "onboardflow-documents";

        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("AccountName=..."))
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType)
    {
        if (_blobServiceClient == null)
        {
            // Fallback for development/testing environments without Azure Blob configured
            return $"https://local-storage-stub.onboardflow.com/{_containerName}/{Guid.NewGuid()}_{fileName}";
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient($"{Guid.NewGuid()}_{fileName}");
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadFileAsync(string blobUri)
    {
        if (_blobServiceClient == null)
        {
            return new MemoryStream();
        }

        var uri = new Uri(blobUri);
        var blobName = Path.GetFileName(uri.LocalPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var download = await blobClient.DownloadStreamingAsync();
        return download.Value.Content;
    }

    public async Task DeleteFileAsync(string blobUri)
    {
        if (_blobServiceClient == null) return;

        var uri = new Uri(blobUri);
        var blobName = Path.GetFileName(uri.LocalPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();
    }
}
