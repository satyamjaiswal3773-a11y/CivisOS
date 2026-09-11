namespace CivisOS.Domain.Constants;

/// <summary>Canonical permission codes used by API policies and React menus.</summary>
public static class AppPermissions
{
    // Users & access
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string PermissionsManage = "permissions.manage";
    public const string RolesManage = "roles.manage";

    // Employees
    public const string EmployeesView = "employees.view";
    public const string EmployeesManage = "employees.manage";

    // Attendance
    public const string AttendanceView = "attendance.view";
    public const string AttendanceManage = "attendance.manage";
    public const string AttendanceApprove = "attendance.approve";
    public const string AttendancePunch = "attendance.punch";
    public const string AttendanceImport = "attendance.import";
    public const string AttendanceLock = "attendance.lock";
    public const string AttendanceReports = "attendance.reports";
    public const string AttendanceDashboard = "attendance.dashboard";
    public const string AttendancePayroll = "attendance.payroll";

    // Shifts
    public const string ShiftsView = "shifts.view";
    public const string ShiftsManage = "shifts.manage";

    // Leave / Holiday
    public const string LeavesView = "leaves.view";
    public const string LeavesManage = "leaves.manage";
    public const string LeavesApprove = "leaves.approve";
    public const string HolidaysManage = "holidays.manage";
    public const string WeeklyOffsManage = "weeklyoffs.manage";

    // Other modules
    public const string GeoFencesView = "geofences.view";
    public const string GeoFencesManage = "geofences.manage";
    public const string VehiclesView = "vehicles.view";
    public const string VehiclesManage = "vehicles.manage";
    public const string CleaningView = "cleaning.view";
    public const string CleaningManage = "cleaning.manage";
    public const string TasksView = "tasks.view";
    public const string TasksManage = "tasks.manage";
    public const string TasksApprove = "tasks.approve";
    public const string ReportsView = "reports.view";
    public const string NotificationsView = "notifications.view";
    public const string MessagingUse = "messaging.use";
    public const string AiView = "ai.view";
    public const string AiManage = "ai.manage";

    public static readonly IReadOnlyList<(string Code, string Name, string Module, string? Description)> Catalog =
    [
        (UsersView, "View Users", "Users", "View user accounts"),
        (UsersManage, "Manage Users", "Users", "Create and update users"),
        (PermissionsManage, "Manage Permissions", "Users", "Assign role and user permissions"),
        (RolesManage, "Manage Roles", "Users", "Configure role permission defaults"),

        (EmployeesView, "View Employees", "Employees", null),
        (EmployeesManage, "Manage Employees", "Employees", null),

        (AttendanceView, "View Attendance", "Attendance", null),
        (AttendanceManage, "Manage Attendance", "Attendance", "Process, correct, exceptions"),
        (AttendanceApprove, "Approve Attendance", "Attendance", "Regularization and OT approval"),
        (AttendancePunch, "Punch Attendance", "Attendance", "Self / admin punch"),
        (AttendanceImport, "Import Attendance", "Attendance", null),
        (AttendanceLock, "Lock Attendance", "Attendance", "Finalize / lock / unlock months"),
        (AttendanceReports, "Attendance Reports", "Attendance", null),
        (AttendanceDashboard, "Attendance Dashboard", "Attendance", null),
        (AttendancePayroll, "Attendance Payroll Data", "Attendance", null),

        (ShiftsView, "View Shifts", "Shifts", null),
        (ShiftsManage, "Manage Shifts", "Shifts", null),

        (LeavesView, "View Leaves", "Leave", null),
        (LeavesManage, "Manage Leaves", "Leave", "Submit / configure leave types"),
        (LeavesApprove, "Approve Leaves", "Leave", null),
        (HolidaysManage, "Manage Holidays", "Leave", null),
        (WeeklyOffsManage, "Manage Weekly Offs", "Leave", null),

        (GeoFencesView, "View Geo-fences", "GeoFences", null),
        (GeoFencesManage, "Manage Geo-fences", "GeoFences", null),
        (VehiclesView, "View Vehicles", "Vehicles", null),
        (VehiclesManage, "Manage Vehicles", "Vehicles", null),
        (CleaningView, "View Cleaning", "Cleaning", null),
        (CleaningManage, "Manage Cleaning", "Cleaning", null),
        (TasksView, "View Tasks", "Tasks", null),
        (TasksManage, "Manage Tasks", "Tasks", null),
        (TasksApprove, "Approve Tasks", "Tasks", null),
        (ReportsView, "View Reports", "Reports", null),
        (NotificationsView, "View Notifications", "Notifications", null),
        (MessagingUse, "Use Messaging", "Messaging", null),
        (AiView, "View AI", "AI", null),
        (AiManage, "Manage AI", "AI", null)
    ];

    public static IReadOnlyList<string> ForRole(string role) => role switch
    {
        AppRoles.SuperAdmin => Catalog.Select(x => x.Code).ToArray(),
        AppRoles.SocietyAdmin => Catalog.Select(x => x.Code).ToArray(),
        AppRoles.Supervisor =>
        [
            EmployeesView,
            AttendanceView, AttendanceApprove, AttendancePunch, AttendanceReports, AttendanceDashboard,
            ShiftsView,
            LeavesView, LeavesApprove,
            GeoFencesView,
            VehiclesView,
            CleaningView,
            TasksView, TasksManage, TasksApprove,
            ReportsView,
            NotificationsView,
            MessagingUse,
            AiView
        ],
        AppRoles.Employee =>
        [
            AttendanceView, AttendancePunch,
            LeavesView, LeavesManage,
            NotificationsView,
            MessagingUse,
            TasksView
        ],
        AppRoles.Driver =>
        [
            AttendanceView, AttendancePunch,
            VehiclesView,
            NotificationsView,
            MessagingUse,
            TasksView
        ],
        AppRoles.Security =>
        [
            AttendanceView, AttendancePunch,
            GeoFencesView,
            NotificationsView,
            MessagingUse,
            TasksView
        ],
        AppRoles.Resident =>
        [
            NotificationsView,
            MessagingUse
        ],
        _ => []
    };
}
