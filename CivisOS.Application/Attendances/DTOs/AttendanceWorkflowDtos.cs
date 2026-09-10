using CivisOS.Domain.Enums;

namespace CivisOS.Application.Attendances.DTOs;

// ---------- Regularization ----------
public record RegularizationDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly AttendanceDate,
    DateTime? RequestedInUtc,
    DateTime? RequestedOutUtc,
    string Reason,
    string? Remarks,
    string? AttachmentReference,
    AttendanceApprovalStatus Status,
    string RequestedByUserId,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? RejectedByUserId,
    DateTime? RejectedAtUtc,
    string? ApproverRemarks,
    DateTime CreatedAtUtc);

public record CreateRegularizationRequest(
    Guid? EmployeeId,
    DateOnly AttendanceDate,
    DateTime? RequestedInUtc,
    DateTime? RequestedOutUtc,
    string Reason,
    string? Remarks = null,
    string? AttachmentReference = null);

public record ApprovalActionRequest(string? Remarks = null);

public record RegularizationListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    AttendanceApprovalStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null);

// ---------- Overtime ----------
public record OvertimeRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly OvertimeDate,
    decimal RequestedHours,
    decimal? ApprovedHours,
    string Reason,
    string? Remarks,
    AttendanceApprovalStatus Status,
    bool IsPayable,
    string RequestedByUserId,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? ApproverRemarks,
    DateTime CreatedAtUtc);

public record CreateOvertimeRequest(
    Guid? EmployeeId,
    DateOnly OvertimeDate,
    decimal RequestedHours,
    string Reason,
    string? Remarks = null);

public record ApproveOvertimeRequest(decimal? ApprovedHours = null, string? Remarks = null, bool IsPayable = true);

public record OvertimeListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    AttendanceApprovalStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null);

// ---------- Exceptions ----------
public record AttendanceExceptionDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly AttendanceDate,
    AttendanceExceptionType ExceptionType,
    AttendanceExceptionStatus Status,
    string Message,
    string? Remarks,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    DateTime CreatedAtUtc);

public record ResolveExceptionRequest(string? Remarks = null, AttendanceExceptionStatus Status = AttendanceExceptionStatus.Resolved);

public record ExceptionListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    AttendanceExceptionType? ExceptionType = null,
    AttendanceExceptionStatus? Status = null);

// ---------- Locks ----------
public record AttendanceLockDto(
    Guid Id,
    int Year,
    int Month,
    AttendanceLockStatus Status,
    string? FinalizedByUserId,
    DateTime? FinalizedAtUtc,
    string? LockedByUserId,
    DateTime? LockedAtUtc,
    string? UnlockedByUserId,
    DateTime? UnlockedAtUtc,
    string? UnlockReason,
    string? Remarks);

public record MonthActionRequest(int Year, int Month, string? Remarks = null);
public record UnlockMonthRequest(int Year, int Month, string Reason);

// ---------- Import ----------
public record ImportBatchDto(
    Guid Id,
    string FileName,
    string ContentType,
    AttendanceImportBatchStatus Status,
    string UploadedByUserId,
    int TotalRecords,
    int ValidRecords,
    int InvalidRecords,
    int DuplicateRecords,
    DateTime? ConfirmedAtUtc,
    string? Remarks,
    DateTime CreatedAtUtc,
    IReadOnlyList<ImportErrorDto> Errors);

public record ImportErrorDto(int RowNumber, string? RawData, string ErrorMessage);

public record ImportConfirmResultDto(
    Guid BatchId,
    int TotalRecords,
    int ValidRecords,
    int InvalidRecords,
    int DuplicateRecords,
    int PunchesInserted,
    int DaysProcessed);

// ---------- Dashboard ----------
public record AttendanceDashboardDto(
    int TotalEmployees,
    int Present,
    int Absent,
    int Leave,
    int HalfDay,
    int WeeklyOff,
    int Holiday,
    int Wfh,
    int OnDuty,
    int Late,
    int EarlyLeaving,
    int MissingPunch,
    int Overtime,
    int PendingRegularization,
    int PendingApproval,
    IReadOnlyList<TrendPointDto> AttendanceTrend,
    IReadOnlyList<DepartmentAttendanceDto> DepartmentWiseAttendance,
    IReadOnlyList<TrendPointDto> LateTrend,
    IReadOnlyList<TrendPointDto> OvertimeTrend,
    IReadOnlyList<LeaveUtilizationDto> LeaveUtilization);

public record TrendPointDto(DateOnly Date, int Count, decimal? Value = null);
public record DepartmentAttendanceDto(Guid DepartmentId, string DepartmentName, int Present, int Absent, int Leave, int Total);
public record LeaveUtilizationDto(string LeaveTypeName, decimal Days);

public record DashboardQuery(DateOnly? Date = null, Guid? DepartmentId = null);

// ---------- Reports / Payroll ----------
public record AttendanceReportQuery(
    int PageNumber = 1,
    int PageSize = 50,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? EmployeeId = null,
    Guid? DepartmentId = null,
    Guid? DesignationId = null,
    Guid? ShiftId = null,
    DayAttendanceStatus? Status = null,
    string? SortBy = null,
    bool SortDescending = false);

public record PayrollAttendanceDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    int Year,
    int Month,
    int PresentDays,
    int AbsentDays,
    decimal LeaveDays,
    decimal LopDays,
    int HalfDays,
    int LateDeductionMinutes,
    decimal ApprovedOvertimeHours,
    int HolidayWorkDays,
    int WeeklyOffWorkDays,
    int TotalWorkingMinutes);

public record AuditLogDto(
    Guid Id,
    string? UserId,
    Guid? EmployeeId,
    DateOnly? AttendanceDate,
    AttendanceAuditAction Action,
    string? OldValue,
    string? NewValue,
    string? Reason,
    string? IpAddress,
    DateTime CreatedAtUtc);

public record AuditLogQuery(
    int PageNumber = 1,
    int PageSize = 50,
    Guid? EmployeeId = null,
    AttendanceAuditAction? Action = null,
    DateOnly? From = null,
    DateOnly? To = null);
