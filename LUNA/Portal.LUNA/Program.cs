using Docker.DotNet;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Components;
using Portal.LUNA.Components.Account;
using Portal.LUNA.Data;
using Portal.LUNA.Endpoints;
using Portal.LUNA.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Components + Blazor Server interactivity
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Auth state provider (from template)
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Cookie auth (Identity default) + admin policy
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=luna.db";
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Docker
var dockerUri = builder.Configuration["Docker:Uri"] ?? "unix:///var/run/docker.sock";
builder.Services.AddSingleton<IDockerClient>(_ =>
    new DockerClientConfiguration(new Uri(dockerUri)).CreateClient());

// LUNA Services
builder.Services.AddScoped<IMcpServerService, McpServerService>();
builder.Services.AddScoped<IUserApiKeyService, UserApiKeyService>();
builder.Services.AddScoped<ISandboxService, SandboxService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<IAdminSettingService, AdminSettingService>();

// HTTP context accessor (needed for minimal API endpoints)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

// Minimal API endpoints for MCP servers
app.MapApiEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Identity UI endpoints
app.MapAdditionalIdentityEndpoints();

app.Run();
