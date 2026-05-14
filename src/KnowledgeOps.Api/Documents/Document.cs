using KnowledgeOps.Shared.Documents;

namespace KnowledgeOps.Api.Documents;

public sealed class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string BlobUrl { get; set; } = "";
    public DocumentStatus Status { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
