using CivisOS.Domain.Entities;

namespace CivisOS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<Vehicle> Vehicles { get; }
    IQueryable<VehicleType> VehicleTypes { get; }
    IQueryable<VehicleDocument> VehicleDocuments { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<Department> Departments { get; }
    IQueryable<Designation> Designations { get; }
    IQueryable<Employee> Employees { get; }
    IQueryable<GeoFence> GeoFences { get; }
    IQueryable<Attendance> Attendances { get; }
    IQueryable<VehicleLocation> VehicleLocations { get; }
    IQueryable<VehicleLocationHistory> VehicleLocationHistories { get; }
    IQueryable<VehicleGeoFenceEvent> VehicleGeoFenceEvents { get; }
    IQueryable<CleaningArea> CleaningAreas { get; }
    IQueryable<CleaningSchedule> CleaningSchedules { get; }
    IQueryable<CleaningLog> CleaningLogs { get; }
    IQueryable<CleaningPhoto> CleaningPhotos { get; }
    IQueryable<WorkTask> WorkTasks { get; }
    IQueryable<AppNotification> AppNotifications { get; }
    IQueryable<UserDeviceToken> UserDeviceTokens { get; }
    IQueryable<Conversation> Conversations { get; }
    IQueryable<ConversationMember> ConversationMembers { get; }
    IQueryable<Message> Messages { get; }
    IQueryable<AiAlert> AiAlerts { get; }

    IQueryable<ShiftMaster> ShiftMasters { get; }
    IQueryable<EmployeeShiftAssignment> EmployeeShiftAssignments { get; }
    IQueryable<AttendancePunch> AttendancePunches { get; }
    IQueryable<EmployeeAttendanceDay> EmployeeAttendanceDays { get; }
    IQueryable<AttendanceBreak> AttendanceBreaks { get; }
    IQueryable<AttendanceRegularization> AttendanceRegularizations { get; }
    IQueryable<OvertimeRequest> OvertimeRequests { get; }
    IQueryable<AttendanceExceptionRecord> AttendanceExceptions { get; }
    IQueryable<AttendanceLock> AttendanceLocks { get; }
    IQueryable<AttendanceAuditLog> AttendanceAuditLogs { get; }
    IQueryable<AttendanceImportBatch> AttendanceImportBatches { get; }
    IQueryable<AttendanceImportError> AttendanceImportErrors { get; }
    IQueryable<WeeklyOffRule> WeeklyOffRules { get; }
    IQueryable<HolidayCalendar> HolidayCalendars { get; }
    IQueryable<LeaveType> LeaveTypes { get; }
    IQueryable<LeaveRequest> LeaveRequests { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
