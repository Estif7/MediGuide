using Scalar.AspNetCore;
using MediGuide.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();   // .NET 10 OpenAPI

// EF Core
builder.Services.AddDbContext<MediGuideDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   // serves /openapi/v1.json
    app.MapScalarApiReference();    // serves the nice UI at /scalar
}

// app.UseHttpsRedirection();
app.MapControllers();

// Seed data in Development
if (app.Environment.IsDevelopment())
{
    await DataSeeder.SeedAsync(app.Services);
}

app.Run();