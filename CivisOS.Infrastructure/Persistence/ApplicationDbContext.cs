using CivisOS.Application.Common.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<VehicleDocument> VehicleDocuments => Set<VehicleDocument>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<GeoFence> GeoFences => Set<GeoFence>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<VehicleLocation> VehicleLocations => Set<VehicleLocation>();
    public DbSet<VehicleLocationHistory> VehicleLocationHistories => Set<VehicleLocationHistory>();
    public DbSet<VehicleGeoFenceEvent> VehicleGeoFenceEvents => Set<VehicleGeoFenceEvent>();
    public DbSet<CleaningArea> CleaningAreas => Set<CleaningArea>();
    public DbSet<CleaningSchedule> CleaningSchedules => Set<CleaningSchedule>();
    public DbSet<CleaningLog> CleaningLogs => Set<CleaningLog>();
    public DbSet<CleaningPhoto> CleaningPhotos => Set<CleaningPhoto>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<AiAlert> AiAlerts => Set<AiAlert>();

    public DbSet<ShiftMaster> ShiftMasters => Set<ShiftMaster>();
    public DbSet<EmployeeShiftAssignment> EmployeeShiftAssignments => Set<EmployeeShiftAssignment>();
    public DbSet<AttendancePunch> AttendancePunches => Set<AttendancePunch>();
    public DbSet<EmployeeAttendanceDay> EmployeeAttendanceDays => Set<EmployeeAttendanceDay>();
    public DbSet<AttendanceBreak> AttendanceBreaks => Set<AttendanceBreak>();
    public DbSet<AttendanceRegularization> AttendanceRegularizations => Set<AttendanceRegularization>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<AttendanceExceptionRecord> AttendanceExceptions => Set<AttendanceExceptionRecord>();
    public DbSet<AttendanceLock> AttendanceLocks => Set<AttendanceLock>();
    public DbSet<AttendanceAuditLog> AttendanceAuditLogs => Set<AttendanceAuditLog>();
    public DbSet<AttendanceImportBatch> AttendanceImportBatches => Set<AttendanceImportBatch>();
    public DbSet<AttendanceImportError> AttendanceImportErrors => Set<AttendanceImportError>();
    public DbSet<WeeklyOffRule> WeeklyOffRules => Set<WeeklyOffRule>();
    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<AppPermission> AppPermissions => Set<AppPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<AppPage> AppPages => Set<AppPage>();

    IQueryable<Vehicle> IApplicationDbContext.Vehicles => Vehicles;
    IQueryable<VehicleType> IApplicationDbContext.VehicleTypes => VehicleTypes;
    IQueryable<VehicleDocument> IApplicationDbContext.VehicleDocuments => VehicleDocuments;
    IQueryable<RefreshToken> IApplicationDbContext.RefreshTokens => RefreshTokens;
    IQueryable<Department> IApplicationDbContext.Departments => Departments;
    IQueryable<Designation> IApplicationDbContext.Designations => Designations;
    IQueryable<Employee> IApplicationDbContext.Employees => Employees;
    IQueryable<GeoFence> IApplicationDbContext.GeoFences => GeoFences;
    IQueryable<Attendance> IApplicationDbContext.Attendances => Attendances;
    IQueryable<VehicleLocation> IApplicationDbContext.VehicleLocations => VehicleLocations;
    IQueryable<VehicleLocationHistory> IApplicationDbContext.VehicleLocationHistories => VehicleLocationHistories;
    IQueryable<VehicleGeoFenceEvent> IApplicationDbContext.VehicleGeoFenceEvents => VehicleGeoFenceEvents;
    IQueryable<CleaningArea> IApplicationDbContext.CleaningAreas => CleaningAreas;
    IQueryable<CleaningSchedule> IApplicationDbContext.CleaningSchedules => CleaningSchedules;
    IQueryable<CleaningLog> IApplicationDbContext.CleaningLogs => CleaningLogs;
    IQueryable<CleaningPhoto> IApplicationDbContext.CleaningPhotos => CleaningPhotos;
    IQueryable<WorkTask> IApplicationDbContext.WorkTasks => WorkTasks;
    IQueryable<AppNotification> IApplicationDbContext.AppNotifications => AppNotifications;
    IQueryable<UserDeviceToken> IApplicationDbContext.UserDeviceTokens => UserDeviceTokens;
    IQueryable<Conversation> IApplicationDbContext.Conversations => Conversations;
    IQueryable<ConversationMember> IApplicationDbContext.ConversationMembers => ConversationMembers;
    IQueryable<Message> IApplicationDbContext.Messages => Messages;
    IQueryable<AiAlert> IApplicationDbContext.AiAlerts => AiAlerts;

    IQueryable<ShiftMaster> IApplicationDbContext.ShiftMasters => ShiftMasters;
    IQueryable<EmployeeShiftAssignment> IApplicationDbContext.EmployeeShiftAssignments => EmployeeShiftAssignments;
    IQueryable<AttendancePunch> IApplicationDbContext.AttendancePunches => AttendancePunches;
    IQueryable<EmployeeAttendanceDay> IApplicationDbContext.EmployeeAttendanceDays => EmployeeAttendanceDays;
    IQueryable<AttendanceBreak> IApplicationDbContext.AttendanceBreaks => AttendanceBreaks;
    IQueryable<AttendanceRegularization> IApplicationDbContext.AttendanceRegularizations => AttendanceRegularizations;
    IQueryable<OvertimeRequest> IApplicationDbContext.OvertimeRequests => OvertimeRequests;
    IQueryable<AttendanceExceptionRecord> IApplicationDbContext.AttendanceExceptions => AttendanceExceptions;
    IQueryable<AttendanceLock> IApplicationDbContext.AttendanceLocks => AttendanceLocks;
    IQueryable<AttendanceAuditLog> IApplicationDbContext.AttendanceAuditLogs => AttendanceAuditLogs;
    IQueryable<AttendanceImportBatch> IApplicationDbContext.AttendanceImportBatches => AttendanceImportBatches;
    IQueryable<AttendanceImportError> IApplicationDbContext.AttendanceImportErrors => AttendanceImportErrors;
    IQueryable<WeeklyOffRule> IApplicationDbContext.WeeklyOffRules => WeeklyOffRules;
    IQueryable<HolidayCalendar> IApplicationDbContext.HolidayCalendars => HolidayCalendars;
    IQueryable<LeaveType> IApplicationDbContext.LeaveTypes => LeaveTypes;
    IQueryable<LeaveRequest> IApplicationDbContext.LeaveRequests => LeaveRequests;

    IQueryable<AppPermission> IApplicationDbContext.AppPermissions => AppPermissions;
    IQueryable<RolePermission> IApplicationDbContext.RolePermissions => RolePermissions;
    IQueryable<UserPermission> IApplicationDbContext.UserPermissions => UserPermissions;
    IQueryable<AppPage> IApplicationDbContext.AppPages => AppPages;

    void IApplicationDbContext.Add<TEntity>(TEntity entity) => Set<TEntity>().Add(entity);
    void IApplicationDbContext.Update<TEntity>(TEntity entity) => Set<TEntity>().Update(entity);
    void IApplicationDbContext.Remove<TEntity>(TEntity entity) => Set<TEntity>().Remove(entity);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
