using MediGuide.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediGuide.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MediGuideDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MediGuideDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            if (await context.ServiceCategories.AnyAsync())
                return; // already seeded

            var categories = new List<ServiceCategory>
            {
                new()
                {
                    Name = "Mental Health",
                    NameAmharic = "የአእምሮ ጤና",
                    Description = "Counseling and psychotherapy support",
                    BasePrice = 1500.00m
                },
                new()
                {
                    Name = "Pediatrics",
                    NameAmharic = "የህፃናት ህክምና",
                    Description = "Child health consultations",
                    BasePrice = 1200.00m
                },
                new()
                {
                    Name = "Nutrition & Diet",
                    NameAmharic = "ስነ-ምግብ",
                    Description = "Dietary planning and nutrition advice",
                    BasePrice = 1000.00m
                },
                new()
                {
                    Name = "General Consultation",
                    NameAmharic = "አጠቃላይ ምክክር",
                    Description = "General medical consultation",
                    BasePrice = 800.00m
                }
            };

            var patient = new Patient
            {
                FullName = "Abeba Tesfaye",
                Email = "abeba@example.com",
                PhoneNumber = "+251911000001",
                PreferredLanguage = "en"
            };

            var agent = new Agent
            {
                FullName = "Dr. Helen Mekonnen",
                Email = "helen@mediguide.et",
                PhoneNumber = "+251911000002",
                IsAvailable = true
            };

            context.ServiceCategories.AddRange(categories);
            context.Patients.Add(patient);
            context.Agents.Add(agent);

            await context.SaveChangesAsync();
            logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}