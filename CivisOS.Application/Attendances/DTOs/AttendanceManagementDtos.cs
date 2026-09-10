using CivisOS.Domain.Enums;

namespace CivisOS.Application.Attendances.DTOs;

// ---------- Shifts ----------
public record ShiftDto(
    Guid Id,
    string ShiftCode,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int GracePeriodMinutes,
    int MinimumWorkingMinutes,
    int AllowedBreakMinutes,
    int LateAfterMinutes,
    int EarlyLeavingAfterMinutes,
    bool OvertimeAllowed,
    int OvertimeAfterMinutes,
    bool IsNightShift,
    bool IsCrossMidnight,
    bool IsActive,
    string? Description,
    DateTime CreatedAtUtc);

public record CreateShiftRequest(
    string ShiftCode,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int GracePeriodMinutes = 15,
    int MinimumWorkingMinutes = 480,
    int AllowedBreakMinutes = 60,
    int LateAfterMinutes = 15,
    int EarlyLeavingAfterMinutes = 15,
    bool OvertimeAllowed = true,
    int OvertimeAfterMinutes = 30,
    bool IsNightShift = false,
    bool IsCrossMidnight = false,
    string? Description = null);

public record UpdateShiftRequest(
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int GracePeriodMinutes,
    int MinimumWorkingMinutes,
    int AllowedBreakMinutes,
    int LateAfterMinutes,
    int EarlyLeavingAfterMinutes,
    bool OvertimeAllowed,
    int OvertimeAfterMinutes,
    bool IsNightShift,
    bool IsCrossMidnight,
    bool IsActive,
    string? Description);

public record AssignShiftRequest(
    Guid EmployeeId,
    Guid ShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    Guid? DepartmentId = null,
    string? Remarks = null);

public record BulkAssignShiftRequest(
    IReadOnlyList<Guid> EmployeeIds,
    Guid ShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    string? Remarks = null);

public record EmployeeShiftDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid ShiftId,
    string ShiftName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? Remarks);

public record ShiftListQuery(int PageNumber = 1, int PageSize = 20, string? Search = null, bool? IsActive = null);

// ---------- Punches ----------
public record AttendancePunchDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateTime PunchDateTimeUtc,
    PunchType PunchType,
    PunchSource Source,
    string? DeviceId,
    string? IpAddress,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    string? Remarks,
    DateTime CreatedAtUtc);

public record CreatePunchRequest(
    Guid EmployeeId,
    DateTime PunchDateTimeUtc,
    PunchType PunchType,
    PunchSource Source = PunchSource.Admin,
    string? DeviceId = null,
    string? IpAddress = null,
    double? Latitude = null,
    double? Longitude = null,
    double? AccuracyMeters = null,
    string? Remarks = null);

public record SelfPunchRequest(
    PunchType PunchType,
    PunchSource Source = PunchSource.Mobile,
    string? DeviceId = null,
    double? Latitude = null,
    double? Longitude = null,
    double? AccuracyMeters = null,
    string? Remarks = null);

public record PunchListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    PunchSource? Source = null,
    PunchType? PunchType = null);

// ---------- Processed daily ----------
public record EmployeeAttendanceDayDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? DesignationId,
    string? DesignationName,
    DateOnly AttendanceDate,
    Guid? ShiftId,
    string? ShiftName,
    DateTime? FirstInUtc,
    DateTime? LastOutUtc,
    int TotalPunches,
    int BreakMinutes,
    int WorkingMinutes,
    int LateMinutes,
    int EarlyOutMinutes,
    int OvertimeMinutes,
    int ExcessBreakMinutes,
    DayAttendanceStatus Status,
    PunchSource? AttendanceSource,
    bool IsLate,
    bool IsEarlyLeaving,
    bool HasMissingPunch,
    bool IsFinalized,
    bool IsManualOverride,
    string? Remarks);

public record DailyAttendanceQuery(
    int PageNumber = 1,
    int PageSize = 20,
    DateOnly? Date = null,
    Guid? EmployeeId = null,
    Guid? DepartmentId = null,
    Guid? DesignationId = null,
    Guid? ShiftId = null,
    DayAttendanceStatus? Status = null,
    PunchSource? Source = null);

public record MonthlyAttendanceQuery(
    int Year,
    int Month,
    int PageNumber = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    Guid? DepartmentId = null,
    Guid? DesignationId = null,
    Guid? ShiftId = null,
    DayAttendanceStatus? Status = null);

public record MonthlyAttendanceEmployeeDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid? DepartmentId,
    string? DepartmentName,
    IReadOnlyList<DayStatusCellDto> Days,
    AttendanceSummaryDto Summary);

public record DayStatusCellDto(
    DateOnly Date,
    DayAttendanceStatus? Status,
    int WorkingMinutes,
    int LateMinutes,
    int OvertimeMinutes,
    bool IsLate,
    bool HasOt);

public record AttendanceSummaryDto(
    int PresentDays,
    int AbsentDays,
    decimal LeaveDays,
    int HalfDays,
    int WeeklyOffDays,
    int HolidayDays,
    int LateCount,
    int EarlyLeavingCount,
    int MissingPunchCount,
    int TotalWorkingMinutes,
    int TotalBreakMinutes,
    int TotalOvertimeMinutes,
    int WfhDays,
    int OnDutyDays);

public record ProcessAttendanceRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? EmployeeId = null,
    Guid? DepartmentId = null);

public record ManualAttendanceCorrectionRequest(
    Guid EmployeeId,
    DateOnly AttendanceDate,
    DateTime? FirstInUtc,
    DateTime? LastOutUtc,
    DayAttendanceStatus Status,
    string Reason,
    int? WorkingMinutes = null,
    int? LateMinutes = null,
    int? EarlyOutMinutes = null,
    int? OvertimeMinutes = null,
    string? Remarks = null);
