using CivisOS.Application.Common.Models;
using CivisOS.Application.Reports.DTOs;

namespace CivisOS.Application.Reports.Interfaces;

public interface IReportService
{
    Task<ApiResponse<VehicleReportDto>> GetVehicleReportAsync(ReportDateRangeQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceReportDto>> GetAttendanceReportAsync(ReportDateRangeQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningReportDto>> GetCleaningReportAsync(ReportDateRangeQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<TaskReportDto>> GetTaskReportAsync(ReportDateRangeQuery query, CancellationToken cancellationToken = default);
}
