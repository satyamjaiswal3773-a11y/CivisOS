using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Enums;

namespace CivisOS.Application.Attendances.Interfaces;

public interface IShiftService
{
    Task<ApiResponse<PagedResult<ShiftDto>>> GetShiftsAsync(ShiftListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<ShiftDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ShiftDto>> CreateAsync(CreateShiftRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ShiftDto>> UpdateAsync(Guid id, UpdateShiftRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeShiftDto>> AssignAsync(string actorUserId, AssignShiftRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> BulkAssignAsync(string actorUserId, BulkAssignShiftRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeShiftDto>> GetEmployeeShiftAsync(Guid employeeId, DateOnly? asOf = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<EmployeeShiftDto>>> GetShiftHistoryAsync(Guid employeeId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public interface IAttendancePunchService
{
    Task<ApiResponse<AttendancePunchDto>> CreatePunchAsync(string actorUserId, CreatePunchRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendancePunchDto>> SelfPunchAsync(string userId, SelfPunchRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<AttendancePunchDto>>> GetPunchesAsync(PunchListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<AttendancePunchDto>>> GetPunchesForDayAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
}

public interface IAttendanceProcessor
{
    Task<ApiResponse<EmployeeAttendanceDayDto>> ProcessEmployeeDayAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessRangeAsync(ProcessAttendanceRequest request, CancellationToken cancellationToken = default);
}

public interface IAttendanceManagementService
{
    Task<ApiResponse<PagedResult<EmployeeAttendanceDayDto>>> GetDailyAsync(DailyAttendanceQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeAttendanceDayDto>> GetEmployeeDayAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<MonthlyAttendanceEmployeeDto>>> GetMonthlyAsync(MonthlyAttendanceQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceSummaryDto>> GetSummaryAsync(Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeAttendanceDayDto>> CorrectAsync(string actorUserId, ManualAttendanceCorrectionRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
}

public interface IAttendanceRegularizationService
{
    Task<ApiResponse<RegularizationDto>> SubmitAsync(string userId, CreateRegularizationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<RegularizationDto>>> GetAsync(RegularizationListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<RegularizationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<RegularizationDto>> ApproveAsync(string approverUserId, Guid id, ApprovalActionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RegularizationDto>> RejectAsync(string approverUserId, Guid id, ApprovalActionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RegularizationDto>> SendBackAsync(string approverUserId, Guid id, ApprovalActionRequest request, CancellationToken cancellationToken = default);
}

public interface IOvertimeService
{
    Task<ApiResponse<OvertimeRequestDto>> SubmitAsync(string userId, CreateOvertimeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<OvertimeRequestDto>>> GetAsync(OvertimeListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<OvertimeRequestDto>> ApproveAsync(string approverUserId, Guid id, ApproveOvertimeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<OvertimeRequestDto>> RejectAsync(string approverUserId, Guid id, ApprovalActionRequest request, CancellationToken cancellationToken = default);
}

public interface IAttendanceExceptionService
{
    Task<ApiResponse<PagedResult<AttendanceExceptionDto>>> GetAsync(ExceptionListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceExceptionDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceExceptionDto>> ResolveAsync(string userId, Guid id, ResolveExceptionRequest request, CancellationToken cancellationToken = default);
}

public interface IAttendanceLockService
{
    Task<ApiResponse<AttendanceLockDto>> FinalizeMonthAsync(string userId, MonthActionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceLockDto>> LockMonthAsync(string userId, MonthActionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceLockDto>> UnlockMonthAsync(string userId, UnlockMonthRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<AttendanceLockDto>>> GetLocksAsync(int? year = null, CancellationToken cancellationToken = default);
    Task<bool> IsMonthLockedAsync(int year, int month, CancellationToken cancellationToken = default);
}

public interface IAttendanceImportService
{
    Task<ApiResponse<ImportBatchDto>> UploadAndValidateAsync(string userId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiResponse<ImportBatchDto>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ImportConfirmResultDto>> ConfirmAsync(string userId, Guid batchId, CancellationToken cancellationToken = default);
}

public interface IAttendanceDashboardService
{
    Task<ApiResponse<AttendanceDashboardDto>> GetDashboardAsync(DashboardQuery query, CancellationToken cancellationToken = default);
}

public interface IAttendanceReportService
{
    Task<ApiResponse<PagedResult<EmployeeAttendanceDayDto>>> GetReportAsync(string reportType, AttendanceReportQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<byte[]>> ExportAsync(string reportType, AttendanceReportQuery query, string format, CancellationToken cancellationToken = default);
}

public interface IAttendancePayrollService
{
    Task<ApiResponse<IReadOnlyList<PayrollAttendanceDto>>> GetPayrollDataAsync(int year, int month, Guid? departmentId = null, Guid? employeeId = null, CancellationToken cancellationToken = default);
}

public interface IAttendanceAuditService
{
    Task LogAsync(
        AttendanceAuditAction action,
        string? userId,
        Guid? employeeId,
        DateOnly? attendanceDate,
        string? oldValue,
        string? newValue,
        string? reason,
        string? ipAddress,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<AuditLogDto>>> GetAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
