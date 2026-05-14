using KnowledgeOps.Api.Documents;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Api.Persistence;

public sealed class KnowledgeOpsDbContext(DbContextOptions<KnowledgeOpsDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var doc = modelBuilder.Entity<Document>();
        doc.ToTable("Documents");
        doc.HasKey(d => d.Id);
        doc.Property(d => d.FileName).HasMaxLength(512).IsRequired();
        doc.Property(d => d.BlobUrl).HasMaxLength(2048).IsRequired();
        doc.Property(d => d.UploadedBy).HasMaxLength(256).IsRequired();
        doc.Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        doc.Property(d => d.CreatedAt).IsRequired();
    }
}
