using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Database.Entities;

namespace Portal.LUNA.Database;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AvailableMcpServer> AvailableMcpServers { get; set; }
    public DbSet<UserApiKey> UserApiKeys { get; set; }
    public DbSet<UserApiKeySetting> UserApiKeySettings { get; set; }
    public DbSet<Sandbox> Sandboxes { get; set; }
    public DbSet<AdminSetting> AdminSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AvailableMcpServer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<UserApiKey>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User)
             .WithMany(u => u.UserApiKeys)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.McpServer)
             .WithMany(m => m.UserApiKeys)
             .HasForeignKey(x => x.McpServerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserApiKeySetting>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.UserApiKey)
             .WithMany(k => k.Settings)
             .HasForeignKey(x => x.UserApiKeyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Sandbox>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.UserApiKey)
             .WithMany(k => k.Sandboxes)
             .HasForeignKey(x => x.UserApiKeyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdminSetting>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
        });
    }
}
