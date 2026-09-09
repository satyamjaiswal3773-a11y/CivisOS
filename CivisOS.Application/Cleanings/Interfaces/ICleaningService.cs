using CivisOS.Application.Cleanings.DTOs;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Enums;

namespace CivisOS.Application.Cleanings.Interfaces;

public interface ICleaningService
{
    Task<ApiResponse<PagedResult<CleaningAreaDto>>> GetAreasAsync(CleaningAreaListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningAreaDto>> GetAreaByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningAreaDto>> CreateAreaAsync(CreateCleaningAreaRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningAreaDto>> UpdateAreaAsync(Guid id, UpdateCleaningAreaRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<CleaningScheduleDto>>> GetSchedulesAsync(int pageNumber, int pageSize, Guid? cleaningAreaId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningScheduleDto>> CreateScheduleAsync(CreateCleaningScheduleRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<CleaningLogDto>>> GetLogsAsync(CleaningLogListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningLogDto>> CreateLogAsync(string userId, CreateCleaningLogRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningLogDto>> CompleteLogAsync(Guid id, CompleteCleaningLogRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningPhotoDto>> AddPhotoAsync(
        Guid logId,
        CleaningPhotoType photoType,
        Stream fileStream,
        string fileName,
        string contentType,
        string? caption,
        CancellationToken cancellationToken = default);
}
