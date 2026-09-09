using CivisOS.Application.Common.Models;
using CivisOS.Application.Tasks.DTOs;

namespace CivisOS.Application.Tasks.Interfaces;

public interface IWorkTaskService
{
    Task<ApiResponse<PagedResult<WorkTaskDto>>> GetTasksAsync(WorkTaskListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkTaskDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkTaskDto>> CreateAsync(string actorUserId, CreateWorkTaskRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkTaskDto>> UpdateAsync(Guid id, UpdateWorkTaskRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkTaskDto>> AssignAsync(string actorUserId, Guid id, AssignWorkTaskRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkTaskDto>> UpdateStatusAsync(Guid id, UpdateWorkTaskStatusRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkTaskDto>> StartAsync(string userId, Guid id, StartWorkTaskRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<WorkTaskDto>>> GetMyTasksAsync(string userId, WorkTaskListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkTaskDto>> ApproveAsync(string actorUserId, Guid id, CancellationToken cancellationToken = default);
}
