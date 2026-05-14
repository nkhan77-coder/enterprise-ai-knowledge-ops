namespace KnowledgeOps.Api.Storage;

public interface IBlobUploader
{
    Task<Uri> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken);
}
