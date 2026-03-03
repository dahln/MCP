using Docker.DotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portal.LUNA.Database;
using Portal.LUNA.Database.Entities;
using Portal.LUNA.Service;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "luna.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "LUNA_DEFAULT_SECRET_KEY_CHANGE_IN_PRODUCTION_12345";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Portal.LUNA";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Portal.LUNA";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));

// Docker
var dockerUri = builder.Configuration["Docker:Uri"] ?? "unix:///var/run/docker.sock";
builder.Services.AddSingleton<IDockerClient>(_ =>
    new DockerClientConfiguration(new Uri(dockerUri)).CreateClient());

// Services
builder.Services.AddScoped<IMcpServerService, McpServerService>();
builder.Services.AddScoped<IUserApiKeyService, UserApiKeyService>();
builder.Services.AddScoped<ISandboxService, SandboxService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<IAdminSettingService, AdminSettingService>();

// CORS - allow Blazor WASM
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Host Blazor WASM app
builder.Services.AddSpaStaticFiles(config => config.RootPath = "wwwroot");

var app = builder.Build();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Serve Blazor WASM
app.UseStaticFiles();
app.UseSpaStaticFiles();
app.UseSpa(spa => spa.Options.SourcePath = "ClientApp");

app.Run();
