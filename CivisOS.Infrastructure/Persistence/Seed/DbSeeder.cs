using CivisOS.Domain.Constants;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
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

            // Sample society coordinates (approx. Hyderabad area) for Swagger/check demos.
            if (!await db.GeoFences.AnyAsync())
            {
                db.GeoFences.AddRange(
                    new GeoFence
                    {
                        Name = "Main Gate",
                        Description = "Society main entrance gate",
                        CenterLatitude = 17.4485,
                        CenterLongitude = 78.3908,
                        RadiusMeters = 40,
                        Status = GeoFenceStatus.Active
                    },
                    new GeoFence
                    {
                        Name = "Parking",
                        Description = "Visitor and resident parking",
                        CenterLatitude = 17.4490,
                        CenterLongitude = 78.3915,
                        RadiusMeters = 80,
                        Status = GeoFenceStatus.Active
                    },
                    new GeoFence
                    {
                        Name = "Office",
                        Description = "Society office / attendance zone",
                        CenterLatitude = 17.4488,
                        CenterLongitude = 78.3910,
                        RadiusMeters = 50,
                        Status = GeoFenceStatus.Active
                    },
                    new GeoFence
                    {
                        Name = "Cleaning Zone A",
                        Description = "Block A common areas",
                        CenterLatitude = 17.4495,
                        CenterLongitude = 78.3905,
                        RadiusMeters = 60,
                        Status = GeoFenceStatus.Active
                    },
                    new GeoFence
                    {
                        Name = "Cleaning Zone B",
                        Description = "Block B common areas",
                        CenterLatitude = 17.4478,
                        CenterLongitude = 78.3920,
                        RadiusMeters = 60,
                        Status = GeoFenceStatus.Active
                    }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.CleaningAreas.AnyAsync())
            {
                var zoneA = await db.GeoFences.FirstOrDefaultAsync(f => f.Name == "Cleaning Zone A");
                var zoneB = await db.GeoFences.FirstOrDefaultAsync(f => f.Name == "Cleaning Zone B");

                db.CleaningAreas.AddRange(
                    new CleaningArea
                    {
                        Name = "Block A Commons",
                        Description = "Lobbies and corridors in Block A",
                        GeoFenceId = zoneA?.Id,
                        Frequency = CleaningFrequency.Daily,
                        NextCleanDueAtUtc = DateTime.UtcNow.AddDays(1),
                        Status = CleaningAreaStatus.Active
                    },
                    new CleaningArea
                    {
                        Name = "Block B Commons",
                        Description = "Lobbies and corridors in Block B",
                        GeoFenceId = zoneB?.Id,
                        Frequency = CleaningFrequency.Daily,
                        NextCleanDueAtUtc = DateTime.UtcNow.AddDays(1),
                        Status = CleaningAreaStatus.Active
                    }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.ShiftMasters.AnyAsync())
            {
                db.ShiftMasters.AddRange(
                    new ShiftMaster
                    {
                        ShiftCode = "GEN",
                        ShiftName = "General Shift",
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(18, 0),
                        GracePeriodMinutes = 15,
                        MinimumWorkingMinutes = 480,
                        AllowedBreakMinutes = 60,
                        LateAfterMinutes = 15,
                        EarlyLeavingAfterMinutes = 15,
                        OvertimeAllowed = true,
                        OvertimeAfterMinutes = 30
                    },
                    new ShiftMaster
                    {
                        ShiftCode = "NIGHT",
                        ShiftName = "Night Shift",
                        StartTime = new TimeOnly(22, 0),
                        EndTime = new TimeOnly(6, 0),
                        GracePeriodMinutes = 15,
                        MinimumWorkingMinutes = 480,
                        AllowedBreakMinutes = 60,
                        IsNightShift = true,
                        IsCrossMidnight = true,
                        OvertimeAllowed = true,
                        OvertimeAfterMinutes = 30
                    }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.LeaveTypes.AnyAsync())
            {
                db.LeaveTypes.AddRange(
                    new LeaveType { Code = "CL", Name = "Casual Leave", IsPaid = true },
                    new LeaveType { Code = "SL", Name = "Sick Leave", IsPaid = true },
                    new LeaveType { Code = "EL", Name = "Earned Leave", IsPaid = true },
                    new LeaveType { Code = "LOP", Name = "Loss of Pay", IsPaid = false }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.WeeklyOffRules.AnyAsync())
            {
                db.WeeklyOffRules.Add(new WeeklyOffRule
                {
                    Name = "Default Sunday Off",
                    Pattern = WeeklyOffPattern.FixedDays,
                    FixedDaysCsv = "0",
                    EffectiveFrom = new DateOnly(2020, 1, 1),
                    IsActive = true,
                    Remarks = "Global default weekly off (Sunday). Override per employee/department as needed."
                });
                await db.SaveChangesAsync();
            }

            var permissionService = sp.GetRequiredService<CivisOS.Application.Permissions.Interfaces.IPermissionService>();
            await permissionService.EnsureSeededAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
