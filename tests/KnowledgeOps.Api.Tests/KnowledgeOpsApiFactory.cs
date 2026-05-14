using KnowledgeOps.Api.Persistence;
using KnowledgeOps.Api.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KnowledgeOps.Api.Tests;

public sealed class KnowledgeOpsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"kops-tests-{Guid.NewGuid():N}";

    public StubBlobUploader BlobUploader { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<KnowledgeOpsDbContext>>();
            services.AddDbContext<KnowledgeOpsDbContext>(o =>
                o.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IBlobUploader>();
            services.AddSingleton<IBlobUploader>(BlobUploader);

            var initializerDescriptors = services
                .Where(d => d.ImplementationType == typeof(BlobContainerInitializer))
                .ToList();
            foreach (var d in initializerDescriptors) services.Remove(d);
        });
    }

    public void Reset()
    {
        BlobUploader.Uploads.Clear();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeOpsDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}

public sealed class StubBlobUploader : IBlobUploader
{
    public List<(string Name, string ContentType, long Length)> Uploads { get; } = new();

    public Task<Uri> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        content.CopyTo(ms);
        Uploads.Add((blobName, contentType, ms.Length));
        return Task.FromResult(new Uri($"https://stub.blob/raw/{blobName}"));
    }
}
