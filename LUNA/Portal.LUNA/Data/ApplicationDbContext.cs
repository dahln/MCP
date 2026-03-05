using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Portal.LUNA.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AvailableMcpServer> AvailableMcpServers { get; set; }
    public DbSet<McpServerInstance> McpServerInstances { get; set; }
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

        builder.Entity<McpServerInstance>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.AvailableMcpServer)
             .WithMany(m => m.Instances)
             .HasForeignKey(x => x.AvailableMcpServerId)
             .OnDelete(DeleteBehavior.Cascade);
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
