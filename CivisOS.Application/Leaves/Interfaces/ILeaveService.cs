using CivisOS.Application.Common.Models;
using CivisOS.Application.Leaves.DTOs;

namespace CivisOS.Application.Leaves.Interfaces;

public interface ILeaveService
{
    Task<ApiResponse<IReadOnlyList<LeaveTypeDto>>> GetLeaveTypesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<LeaveTypeDto>> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LeaveTypeDto>> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LeaveRequestDto>> SubmitAsync(string userId, CreateLeaveRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<LeaveRequestDto>>> GetAsync(LeaveListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<LeaveRequestDto>> ApproveAsync(string approverUserId, Guid id, LeaveApprovalRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LeaveRequestDto>> RejectAsync(string approverUserId, Guid id, LeaveApprovalRequest request, CancellationToken cancellationToken = default);
    Task<bool> HasApprovedLeaveAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
}
