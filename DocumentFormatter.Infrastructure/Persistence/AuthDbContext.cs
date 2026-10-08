using DocumentFormatter.Infrastructure.Authentication;
using DocumentFormatter.Domain.Chats;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocumentFormatter.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<ChatDocument> ChatDocuments => Set<ChatDocument>();

    public DbSet<PredefinedAction> PredefinedActions => Set<PredefinedAction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>()
            .Property(user => user.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Entity<ChatThread>(entity =>
        {
            entity.HasKey(chat => chat.Id);
            entity.Property(chat => chat.Title).HasMaxLength(200).IsRequired();
            entity.Property(chat => chat.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(chat => new { chat.OwnerUserId, chat.UpdatedAtUtc });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(chat => chat.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(chat => chat.Messages)
                .WithOne()
                .HasForeignKey(message => message.ChatThreadId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(chat => chat.Documents)
                .WithOne()
                .HasForeignKey(document => document.ChatThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Role).HasMaxLength(20).IsRequired();
            entity.Property(message => message.Content).HasMaxLength(10000).IsRequired();
            entity.HasIndex(message => new { message.ChatThreadId, message.CreatedAtUtc });
        });

        builder.Entity<ChatDocument>(entity =>
        {
            entity.HasKey(document => document.Id);
            entity.Property(document => document.FileName).HasMaxLength(255).IsRequired();
            entity.Property(document => document.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(document => document.Extension).HasMaxLength(10).IsRequired();
            entity.Property(document => document.Content).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(document => document.Length).IsRequired();
            entity.HasIndex(document => new { document.ChatThreadId, document.CreatedAtUtc });
        });

        builder.Entity<PredefinedAction>(entity =>
        {
            entity.HasKey(action => action.Id);
            entity.Property(action => action.Label).HasMaxLength(100).IsRequired();
            entity.Property(action => action.Prompt).HasMaxLength(2000).IsRequired();
            entity.Property(action => action.OwnerUserId).HasMaxLength(450);
            entity.HasIndex(action => new { action.OwnerUserId, action.IsActive, action.SortOrder });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(action => action.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(
                new PredefinedAction
                {
                    Id = 1,
                    Label = "Format this page",
                    Prompt = "Format this document to improve its layout and consistency while preserving its content.",
                    SortOrder = 1
                },
                new PredefinedAction
                {
                    Id = 2,
                    Label = "Resize this page",
                    Prompt = "Resize the pages in this document to a suitable standard page size while preserving the content.",
                    SortOrder = 2
                },
                new PredefinedAction
                {
                    Id = 3,
                    Label = "Change colour combination",
                    Prompt = "Update the document colour combination to a professional, accessible palette while preserving its content.",
                    SortOrder = 3
                },
                new PredefinedAction
                {
                    Id = 4,
                    Label = "Adjust margin",
                    Prompt = "Adjust the document margins for a clean, balanced layout while preserving its content.",
                    SortOrder = 4
                });
        });
    }
}