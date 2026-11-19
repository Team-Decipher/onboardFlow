using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

var app = builder.Build();

// Auto-migrate & seed database
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetService<AppDbContext>();
    if (db != null)
    {
        db.Database.EnsureCreated();

        // Ensure PhoneNumber column exists on existing Users table
        try
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PhoneNumber"" character varying(50) DEFAULT '' NOT NULL;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] Column migration note: {ex.Message}");
        }

        if (!db.Users.Any())
        {
            db.Users.AddRange(new List<UserAccount>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Kasun Jayawardena",
                    Email = "kasun@example.com",
                    Role = "Onboarding Manager",
                    Organisation = "Internal team",
                    PhoneNumber = "+94 71 884 9201",
                    Status = "Active",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Amara Silva",
                    Email = "amara@example.com",
                    Role = "Reviewer",
                    Organisation = "Internal team",
                    PhoneNumber = "+94 76 552 1094",
                    Status = "Active",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Nimali Perera",
                    Email = "nimali@example.com",
                    Role = "Client",
                    Organisation = "Ceylon Exports",
                    PhoneNumber = "+94 77 123 4567",
                    Status = "Active",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Ravindu Fernando",
                    Email = "ravindu@example.com",
                    Role = "Reviewer",
                    Organisation = "Internal team",
                    PhoneNumber = "+94 78 440 3391",
                    Status = "Deactivated",
                    CreatedAtUtc = DateTime.UtcNow
                }
            });
            db.SaveChanges();
            Console.WriteLine("[Database] Seeded initial user accounts successfully.");
        }
        else
        {
            // Ensure Nimali Perera has the exact phone number from mockup if currently blank
            var nimali = db.Users.FirstOrDefault(u => u.Email == "nimali@example.com");
            if (nimali != null && string.IsNullOrEmpty(nimali.PhoneNumber))
            {
                nimali.PhoneNumber = "+94 77 123 4567";
                db.SaveChanges();
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Database Warning] Could not initialize or seed database: {ex.Message}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
