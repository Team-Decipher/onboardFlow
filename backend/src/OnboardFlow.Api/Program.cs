using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.UsersAndAccess.Authorization;
using OnboardFlow.Api.Modules.UsersAndAccess.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<ITokenService, TokenService>();

// Database connection
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// CORS setup
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- JWT Authentication Configuration ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-change-me-use-at-least-32-characters";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OnboardFlow";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OnboardFlowClient";

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetService<AppDbContext>();
    if (db != null)
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OnboardFlow API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Authentication MUST be before Authorization!
app.UseAuthentication();
app.UseMiddleware<ActiveUserMiddleware>(); // 🔒 Rejects deactivated users on all authenticated requests
app.UseAuthorization();

app.MapControllers();

// Seed initial database records
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(dbContext);
}

app.Run();
