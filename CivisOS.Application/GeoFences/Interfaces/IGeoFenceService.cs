using CivisOS.Application.Common.Models;
using CivisOS.Application.GeoFences.DTOs;

namespace CivisOS.Application.GeoFences.Interfaces;

public interface IGeoFenceService
{
    Task<ApiResponse<PagedResult<GeoFenceDto>>> GetGeoFencesAsync(GeoFenceListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<GeoFenceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<GeoFenceDto>> CreateAsync(CreateGeoFenceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<GeoFenceDto>> UpdateAsync(Guid id, UpdateGeoFenceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<GeoFenceCheckResultDto>> CheckAsync(GeoFenceCheckRequest request, CancellationToken cancellationToken = default);
}
