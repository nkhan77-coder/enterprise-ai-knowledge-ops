using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KnowledgeOps.Api.Persistence;
using KnowledgeOps.Shared.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeOps.Api.Tests;

public sealed class DocumentUploadTests : IClassFixture<KnowledgeOpsApiFactory>
{
    private readonly KnowledgeOpsApiFactory _factory;
    private readonly HttpClient _client;

    public DocumentUploadTests(KnowledgeOpsApiFactory factory)
    {
        _factory = factory;
        _factory.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Returns_400_when_file_is_empty()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "empty.pdf");

        var response = await _client.PostAsync("/api/documents/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_415_when_content_type_is_not_pdf()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "doc.txt");

        var response = await _client.PostAsync("/api/documents/upload", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Stores_blob_and_metadata_when_pdf_uploaded()
    {
        var pdfBytes = MakePdfPayload();
        var content = MakePdfMultipart(pdfBytes, "test.pdf");

        var response = await _client.PostAsync("/api/documents/upload", content);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.DocumentId);

        Assert.Single(_factory.BlobUploader.Uploads);
        Assert.Equal("application/pdf", _factory.BlobUploader.Uploads[0].ContentType);
        Assert.Equal(pdfBytes.Length, _factory.BlobUploader.Uploads[0].Length);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
        var doc = await db.Documents.SingleOrDefaultAsync(d => d.Id == body.DocumentId);
        Assert.NotNull(doc);
        Assert.Equal("test.pdf", doc!.FileName);
        Assert.Equal(DocumentStatus.Uploaded, doc.Status);
        Assert.Equal("anonymous", doc.UploadedBy);
        Assert.Contains(doc.Id.ToString(), doc.BlobUrl);

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task Echoes_inbound_correlation_id()
    {
        var pdfBytes = MakePdfPayload();
        var content = MakePdfMultipart(pdfBytes, "test.pdf");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/upload") { Content = content };
        request.Headers.Add("X-Correlation-ID", "test-corr-123");

        var response = await _client.SendAsync(request);

        Assert.Equal("test-corr-123", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task Records_uploaded_by_when_header_provided()
    {
        var pdfBytes = MakePdfPayload();
        var content = MakePdfMultipart(pdfBytes, "test.pdf");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/upload") { Content = content };
        request.Headers.Add("X-Uploaded-By", "noman@example.com");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UploadDocumentResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
        var doc = await db.Documents.SingleAsync(d => d.Id == body!.DocumentId);
        Assert.Equal("noman@example.com", doc.UploadedBy);
    }

    private static MultipartFormDataContent MakePdfMultipart(byte[] bytes, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private static byte[] MakePdfPayload() =>
        Encoding.ASCII.GetBytes("%PDF-1.4\nfake-pdf-content-for-testing\n%%EOF");
}
