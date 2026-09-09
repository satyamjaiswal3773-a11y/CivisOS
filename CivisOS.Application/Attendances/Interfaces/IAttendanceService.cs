using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Common.Models;

namespace CivisOS.Application.Attendances.Interfaces;

public interface IAttendanceService
{
    Task<ApiResponse<AttendanceDto>> CheckInAsync(string userId, AttendanceCheckInRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceDto>> CheckOutAsync(string userId, AttendanceCheckOutRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<AttendanceDto>>> GetAttendanceAsync(AttendanceListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<AttendanceDto>>> GetMyAttendanceAsync(string userId, AttendanceListQuery query, CancellationToken cancellationToken = default);
}
