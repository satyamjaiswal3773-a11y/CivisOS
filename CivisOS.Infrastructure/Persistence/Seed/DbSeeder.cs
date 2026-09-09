using CivisOS.Domain.Constants;
using CivisOS.Domain.Entities;
using CivisOS.Infrastructure.Identity;
using CivisOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CivisOS.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        try
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in AppRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            const string adminEmail = "admin@civisos.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Admin"
                };

                var result = await userManager.CreateAsync(admin, "Admin@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.SuperAdmin);
                }
                else
                {
                    logger.LogWarning("Failed to seed admin user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            if (!await db.VehicleTypes.AnyAsync())
            {
                db.VehicleTypes.AddRange(
                    new VehicleType { Name = "Car", Description = "Passenger cars" },
                    new VehicleType { Name = "SUV", Description = "Sport utility vehicles" },
                    new VehicleType { Name = "Van", Description = "Passenger/cargo vans" },
                    new VehicleType { Name = "Truck", Description = "Utility trucks" },
                    new VehicleType { Name = "Bike", Description = "Two-wheelers" },
                    new VehicleType { Name = "Bus", Description = "Society shuttle buses" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Departments.AnyAsync())
            {
                db.Departments.AddRange(
                    new Department { Name = "Administration", Description = "Society office and admin staff" },
                    new Department { Name = "Cleaning", Description = "Housekeeping and sanitation" },
                    new Department { Name = "Security", Description = "Gate and patrol security" },
                    new Department { Name = "Maintenance", Description = "Facilities and repairs" },
                    new Department { Name = "Transport", Description = "Vehicle and driver operations" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Designations.AnyAsync())
            {
                db.Designations.AddRange(
                    new Designation { Name = "Manager", Description = "Department manager" },
                    new Designation { Name = "Supervisor", Description = "Team supervisor" },
                    new Designation { Name = "Staff", Description = "General staff" },
                    new Designation { Name = "Driver", Description = "Vehicle driver" },
                    new Designation { Name = "Security Guard", Description = "Security personnel" },
                    new Designation { Name = "Cleaner", Description = "Cleaning staff" }
                );
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
