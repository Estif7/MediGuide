using MediGuide.Domain.Entities;
using Microsoft.AspNetCore.Identity;
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MediGuideDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            // ---------- 1. Roles ----------
            string[] roles = ["Patient", "Agent", "Admin"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created role: {Role}", role);
                }
            }

            // ---------- 2. Default Admin ----------
            const string adminEmail = "admin@mediguide.et";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Admin user created: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create admin: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // ---------- 3. Service Categories + sample Patient/Agent (only if empty) ----------
            if (!await context.ServiceCategories.AnyAsync())
            {
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

                logger.LogInformation("Sample categories, patient and agent seeded.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}