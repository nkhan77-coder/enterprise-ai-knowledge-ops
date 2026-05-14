using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace KnowledgeOps.Api.Storage;

public sealed class AzureBlobUploader(BlobServiceClient service, BlobStorageOptions options) : IBlobUploader
{
    private readonly BlobContainerClient _container = service.GetBlobContainerClient(options.ContainerName);

    public async Task<Uri> UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
        return blob.Uri;
    }
}
