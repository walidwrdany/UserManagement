using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data;


public static class WebApplicationExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<Role>>();

            await DbInitializer.Initialize(context, userManager, roleManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}


public static class DbInitializer
{
    public static async Task Initialize(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger logger)
    {
        logger.LogInformation("Starting database initialization...");

        try
        {
            // Check if we should run migrations
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }

            await SeedData(context, userManager, roleManager);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw; // Re-throw to ensure startup fails if initialization fails
        }

        logger.LogInformation("Database initialization completed successfully.");
    }

    private static async Task SeedData(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager)
    {
        // Your existing seed methods (SeedPermissions, SeedRoles, etc.)

        await SeedPermissions(context);
        await SeedRoles(roleManager);
        await SeedUsers(userManager);
        await SeedUserDetails(context);
    }


    private static async Task SeedPermissions(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        var permissions = new List<Permission>
        {
            new() { Name = "CanViewDashboard", Description = "CanViewDashboard" },
            new() { Name = "CanViewUser", Description = "CanViewUser" },
            new() { Name = "CanEditUser", Description = "CanEditUser" },
            new() { Name = "CanDeleteUser", Description = "CanDeleteUser" },
            new() { Name = "CanCreateUser", Description = "CanCreateUser" },
            new() { Name = "CanViewRole", Description = "CanViewRole" },
            new() { Name = "CanEditRole", Description = "CanEditRole" },
            new() { Name = "CanDeleteRole", Description = "CanDeleteRole" },
            new() { Name = "CanCreateRole", Description = "CanCreateRole" }
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRoles(RoleManager<Role> roleManager)
    {
        if (await roleManager.Roles.AnyAsync()) return;

        var roles = new List<Role>
        {
            new() { Name = "Admin", NormalizedName = "ADMIN" },
            new() { Name = "Manager", NormalizedName = "MANAGER" },
            new() { Name = "User", NormalizedName = "USER" }
        };

        foreach (var role in roles)
        {
            await roleManager.CreateAsync(role);
        }
    }

    private static async Task SeedUsers(UserManager<User> userManager)
    {
        if (await userManager.Users.AnyAsync()) return;

        var users = new List<User>
        {
            new()
            {
                FullName = "admin",
                UserName = "admin@example.com",
                Email = "admin@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                PhoneNumberConfirmed = true
            },
            new()
            {
                FullName = "manager",
                UserName = "manager@example.com",
                Email = "manager@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+1234567891",
                PhoneNumberConfirmed = true
            },
            new()
            {
                FullName = "user1",
                UserName = "user1@example.com",
                Email = "user1@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+1234567892",
                PhoneNumberConfirmed = true
            },
            new()
            {
                FullName = "user2",
                UserName = "user2@example.com",
                Email = "user2@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+1234567893",
                PhoneNumberConfirmed = true
            }
        };

        foreach (var user in users)
        {
            await userManager.CreateAsync(user, "Password123!");
        }
    }

    private static async Task SeedUserDetails(ApplicationDbContext context)
    {
        if (await context.UserDetails.AnyAsync()) return;

        var users = await context.Users.ToListAsync();
        var random = new Random();

        foreach (var user in users)
        {
            var details = new UserDetail
            {
                UserId = user.Id,
                Birthdate = DateTime.Now.AddYears(-random.Next(18, 60)).AddDays(-random.Next(0, 365)),
                Address = $"{random.Next(1, 1000)} {GetRandomStreet()} St",
                UserTypeId = random.Next(1, 4), // Assuming user types are 1, 2, 3
                GenderId = random.Next(1, 2),
                IdentityNumber = $"ID-{random.Next(100000, 999999)}",
                NationalityId = random.Next(1, 20)
            };

            // Extra JSON data

            var userName = $"@{user.UserName?.Split('@')[0]}";

            details.SetExtra(new Dictionary<string, object>
            {
                { "Interests", GetRandomInterests(random) },
                { "Preferences", new { Theme = random.Next(0, 2) == 0 ? "Light" : "Dark", Notifications = true } },
                { "SocialMedia", new { Twitter = userName, Instagram = userName } }
            });

            await context.UserDetails.AddRangeAsync(details);
        }

        await context.SaveChangesAsync();
    }

    private static string GetRandomStreet()
    {
        var streets = new[] { "Main", "Oak", "Pine", "Maple", "Cedar", "Elm", "Birch", "Willow", "Park", "Washington" };
        return streets[new Random().Next(streets.Length)];
    }

    private static List<string> GetRandomInterests(Random random)
    {
        var allInterests = new List<string>
        {
            "Reading", "Sports", "Music", "Travel", "Cooking", "Photography",
            "Gaming", "Movies", "Hiking", "Art", "Technology", "Fitness"
        };

        return allInterests
            .OrderBy(_ => random.Next())
            .Take(random.Next(2, 5))
            .ToList();
    }
}