using System.Text;
using CivisOS.Application.Ai.Interfaces;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Cleanings.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Employees.Interfaces;
using CivisOS.Application.GeoFences.Interfaces;
using CivisOS.Application.Holidays.Interfaces;
using CivisOS.Application.Leaves.Interfaces;
using CivisOS.Application.Messaging.Interfaces;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Application.Reports.Interfaces;
using CivisOS.Application.Tasks.Interfaces;
using CivisOS.Application.Vehicles.Interfaces;
using CivisOS.Infrastructure.Identity;
using CivisOS.Infrastructure.Persistence;
using CivisOS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CivisOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<FcmSettings>(configuration.GetSection(FcmSettings.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

        services.AddAuthentication(options =>
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
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken)
                            && (path.StartsWithSegments("/hubs/notifications")
                                || path.StartsWithSegments("/hubs/chat")))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        services.AddHttpClient("fcm");
        services.AddSignalR();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IGeoFenceService, GeoFenceService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceAuditService, AttendanceAuditService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IAttendanceLockService, AttendanceLockService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<IWeeklyOffService, WeeklyOffService>();
        services.AddScoped<IAttendanceProcessor, AttendanceProcessor>();
        services.AddScoped<IAttendancePunchService, AttendancePunchService>();
        services.AddScoped<IAttendanceManagementService, AttendanceManagementService>();
        services.AddScoped<IAttendanceRegularizationService, AttendanceRegularizationService>();
        services.AddScoped<IOvertimeService, OvertimeService>();
        services.AddScoped<IAttendanceExceptionService, AttendanceExceptionService>();
        services.AddScoped<IAttendanceImportService, AttendanceImportService>();
        services.AddScoped<IAttendanceDashboardService, AttendanceDashboardService>();
        services.AddScoped<IAttendanceReportService, AttendanceReportService>();
        services.AddScoped<IAttendancePayrollService, AttendancePayrollService>();
        services.AddScoped<IVehicleGpsService, VehicleGpsService>();
        services.AddScoped<ICleaningService, CleaningService>();
        services.AddScoped<IWorkTaskService, WorkTaskService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFcmPushService, FcmPushService>();
        services.AddScoped<IMessagingService, MessagingService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAiService, AiService>();

        return services;
    }
}
