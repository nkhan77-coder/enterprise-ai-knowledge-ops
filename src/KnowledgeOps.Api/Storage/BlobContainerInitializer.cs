using Azure.Storage.Blobs;

namespace KnowledgeOps.Api.Storage;

public sealed class BlobContainerInitializer(
    BlobServiceClient service,
    BlobStorageOptions options,
    ILogger<BlobContainerInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var container = service.GetBlobContainerClient(options.ContainerName);
        var response = await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        if (response is not null)
            logger.LogInformation("Created blob container {Container}", options.ContainerName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
