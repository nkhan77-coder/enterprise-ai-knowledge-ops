using Azure.Storage.Blobs;
using KnowledgeOps.Api.Documents;
using KnowledgeOps.Api.Middleware;
using KnowledgeOps.Api.Persistence;
using KnowledgeOps.Api.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(o =>
{
    o.IncludeScopes = true;
    o.SingleLine = true;
});

var blobOptions = builder.Configuration
    .GetSection(BlobStorageOptions.SectionName)
    .Get<BlobStorageOptions>() ?? new BlobStorageOptions();
builder.Services.AddSingleton(blobOptions);

var dbProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
var dbConnection = builder.Configuration.GetConnectionString("KnowledgeOps")
                   ?? "Data Source=knowledgeops.db";

builder.Services.AddDbContext<KnowledgeOpsDbContext>(options =>
{
    if (string.Equals(dbProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(dbConnection);
    else
        options.UseSqlite(dbConnection);
});

builder.Services.AddSingleton(_ => new BlobServiceClient(blobOptions.ConnectionString));
builder.Services.AddSingleton<IBlobUploader, AzureBlobUploader>();
builder.Services.AddHostedService<BlobContainerInitializer>();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = DocumentUploadHandler.MaxUploadBytes;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KnowledgeOps API",
        Version = "v1",
        Description = "Phase 1 — document upload and metadata persistence"
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/documents/upload", DocumentUploadHandler.Handle)
    .WithName("UploadDocument")
    .WithTags("Documents")
    .DisableAntiforgery()
    .Accepts<IFormFile>("multipart/form-data")
    .Produces<KnowledgeOps.Shared.Documents.UploadDocumentResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status413PayloadTooLarge)
    .Produces(StatusCodes.Status415UnsupportedMediaType);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithTags("System");

app.Run();

public partial class Program;
