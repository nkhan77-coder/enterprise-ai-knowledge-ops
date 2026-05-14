using KnowledgeOps.Api.Middleware;
using KnowledgeOps.Api.Persistence;
using KnowledgeOps.Api.Storage;
using KnowledgeOps.Shared.Documents;

namespace KnowledgeOps.Api.Documents;

public static class DocumentUploadHandler
{
    public const long MaxUploadBytes = 25L * 1024 * 1024;
    public const string PdfContentType = "application/pdf";

    public static async Task<IResult> Handle(
        HttpContext context,
        IFormFile? file,
        KnowledgeOpsDbContext db,
        IBlobUploader blob,
        ILogger<DocumentUploadHandlerLog> logger,
        CancellationToken cancellationToken)
    {
        var correlationId = context.GetCorrelationId();

        if (file is null || file.Length == 0)
        {
            logger.LogWarning("Upload rejected: no file or empty file");
            return Results.BadRequest(new { error = "file is required and must not be empty" });
        }

        if (file.Length > MaxUploadBytes)
        {
            logger.LogWarning("Upload rejected: file too large ({Size} bytes)", file.Length);
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        if (!string.Equals(file.ContentType, PdfContentType, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Upload rejected: unsupported content type {ContentType}", file.ContentType);
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        var documentId = Guid.NewGuid();
        var blobName = $"{documentId}.pdf";
        var uploadedBy = context.Request.Headers["X-Uploaded-By"].FirstOrDefault() ?? "anonymous";

        Uri blobUri;
        try
        {
            await using var stream = file.OpenReadStream();
            blobUri = await blob.UploadAsync(blobName, stream, PdfContentType, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blob upload failed for {DocumentId}", documentId);
            return Results.Problem("blob upload failed", statusCode: StatusCodes.Status500InternalServerError);
        }

        try
        {
            db.Documents.Add(new Document
            {
                Id = documentId,
                FileName = file.FileName,
                BlobUrl = blobUri.ToString(),
                Status = DocumentStatus.Uploaded,
                UploadedBy = uploadedBy,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Metadata persistence failed for {DocumentId}; orphan blob at {BlobUrl}", documentId, blobUri);
            return Results.Problem("metadata persistence failed", statusCode: StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation(
            "Document {DocumentId} uploaded ({FileName}, {Size} bytes, correlation={CorrelationId})",
            documentId, file.FileName, file.Length, correlationId);

        return Results.Ok(new UploadDocumentResponse(documentId));
    }
}

public sealed class DocumentUploadHandlerLog;
