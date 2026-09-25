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

            // ---------- 3. Service Categories (Idempotent check) ----------
            var defaultCategories = new List<ServiceCategory>
            {
                new()
                {
                    Name = "General Consultation",
                    NameAmharic = "አጠቃላይ ምክክር",
                    Description = "General medical consultations, primary care & health checkups",
                    BasePrice = 800.00m
                },
                new()
                {
                    Name = "Pediatrics",
                    NameAmharic = "የህፃናት ህክምና",
                    Description = "Child health, growth monitoring and pediatric care",
                    BasePrice = 1200.00m
                },
                new()
                {
                    Name = "Mental Health",
                    NameAmharic = "የአእምሮ ጤና",
                    Description = "Counseling, psychotherapy, and psychiatric support",
                    BasePrice = 1500.00m
                },
                new()
                {
                    Name = "Cardiology",
                    NameAmharic = "የልብ ህክምና",
                    Description = "Heart disease screening, blood pressure & cardiovascular consultations",
                    BasePrice = 1800.00m
                },
                new()
                {
                    Name = "Dermatology",
                    NameAmharic = "የቆዳ ህክምና",
                    Description = "Skin disorders, rash, acne & dermatological evaluations",
                    BasePrice = 1100.00m
                },
                new()
                {
                    Name = "Obstetrics & Gynecology",
                    NameAmharic = "የማህፀንና ፅንስ ህክምና",
                    Description = "Prenatal counseling, reproductive and women's health",
                    BasePrice = 1600.00m
                },
                new()
                {
                    Name = "Nutrition & Diet",
                    NameAmharic = "ስነ-ምግብ",
                    Description = "Dietary planning, metabolic health & nutrition advice",
                    BasePrice = 1000.00m
                }
            };

            foreach (var cat in defaultCategories)
            {
                if (!await context.ServiceCategories.AnyAsync(c => c.Name == cat.Name))
                {
                    context.ServiceCategories.Add(cat);
                }
            }
            await context.SaveChangesAsync();

            // ---------- 4. Specialist Doctors / Medical Agents ----------
            var specialistSeeds = new[]
            {
                new
                {
                    FullName = "Dr. Helen Mekonnen",
                    Email = "helen@mediguide.et",
                    Phone = "+251911000002",
                    Title = "MD, General Physician",
                    Department = "General Practice",
                    Specialty = "Internal Medicine & Primary Care"
                },
                new
                {
                    FullName = "Dr. Dawit Haile",
                    Email = "dawit@mediguide.et",
                    Phone = "+251911000003",
                    Title = "MD, Consultant Pediatrician",
                    Department = "Pediatrics",
                    Specialty = "Child Health & Neonatology"
                },
                new
                {
                    FullName = "Dr. Bethlehem Tadesse",
                    Email = "bethlehem@mediguide.et",
                    Phone = "+251911000004",
                    Title = "MD, Consultant Psychiatrist",
                    Department = "Mental Health",
                    Specialty = "Psychiatry & Clinical Counseling"
                },
                new
                {
                    FullName = "Dr. Yonas Bekele",
                    Email = "yonas@mediguide.et",
                    Phone = "+251911000005",
                    Title = "MD, Cardiologist",
                    Department = "Cardiology",
                    Specialty = "Cardiovascular Medicine & Hypertension"
                },
                new
                {
                    FullName = "Dr. Samrawit Assefa",
                    Email = "samrawit@mediguide.et",
                    Phone = "+251911000006",
                    Title = "MD, Dermatologist",
                    Department = "Dermatology",
                    Specialty = "Clinical & Pediatric Dermatology"
                },
                new
                {
                    FullName = "Dr. Meron Getachew",
                    Email = "meron@mediguide.et",
                    Phone = "+251911000007",
                    Title = "MD, Consultant OB/GYN",
                    Department = "Obstetrics & Gynecology",
                    Specialty = "Maternal-Fetal Medicine & Women's Health"
                }
            };

            const string defaultAgentPassword = "Agent123!";

            foreach (var spec in specialistSeeds)
            {
                var agent = await context.Agents.FirstOrDefaultAsync(a => a.Email == spec.Email);
                if (agent == null)
                {
                    agent = new Agent
                    {
                        FullName = spec.FullName,
                        Email = spec.Email,
                        PhoneNumber = spec.Phone,
                        Title = spec.Title,
                        Department = spec.Department,
                        Specialty = spec.Specialty,
                        IsAvailable = true,
                        IsActive = true
                    };
                    context.Agents.Add(agent);
                    await context.SaveChangesAsync();
                }
                else
                {
                    agent.Title = spec.Title;
                    agent.Department = spec.Department;
                    agent.Specialty = spec.Specialty;
                    agent.IsAvailable = true;
                    agent.IsActive = true;
                    await context.SaveChangesAsync();
                }

                // Ensure Identity User exists for Agent
                var agentUser = await userManager.FindByEmailAsync(spec.Email);
                if (agentUser == null)
                {
                    agentUser = new ApplicationUser
                    {
                        UserName = spec.Email,
                        Email = spec.Email,
                        FullName = spec.FullName,
                        PhoneNumber = spec.Phone,
                        AgentId = agent.Id,
                        EmailConfirmed = true
                    };

                    var userCreateResult = await userManager.CreateAsync(agentUser, defaultAgentPassword);
                    if (userCreateResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(agentUser, "Agent");
                        logger.LogInformation("Created agent user: {Email}", spec.Email);
                    }
                }
                else if (agentUser.AgentId == null)
                {
                    agentUser.AgentId = agent.Id;
                    await userManager.UpdateAsync(agentUser);
                }
            }

            // ---------- 5. Sample Patient ----------
            if (!await context.Patients.AnyAsync())
            {
                var patient = new Patient
                {
                    FullName = "Abeba Tesfaye",
                    Email = "abeba@example.com",
                    PhoneNumber = "+251911000001",
                    PreferredLanguage = "en"
                };
                context.Patients.Add(patient);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Specialist medical departments, doctors, and users seeded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}