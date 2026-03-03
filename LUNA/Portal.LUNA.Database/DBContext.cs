using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Portal.LUNA.Database
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<AvailableMcpServer> AvailableMcpServers { get; set; }
        public DbSet<McpContainerInstance> McpContainerInstances { get; set; }
        public DbSet<UserApiKey> UserApiKeys { get; set; }
        public DbSet<UserApiKeyKvp> UserApiKeyKvps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserApiKey>()
                .HasMany(k => k.KeyValuePairs)
                .WithOne()
                .HasForeignKey(kv => kv.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
