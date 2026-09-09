using CivisOS.Application.Common.Models;
using CivisOS.Application.Employees.DTOs;
using CivisOS.Application.Vehicles.DTOs;

namespace CivisOS.Application.Vehicles.Interfaces;

public interface IVehicleService
{
    Task<ApiResponse<PagedResult<VehicleDto>>> GetVehiclesAsync(VehicleListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleDto>> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleDto>> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleDocumentDto>> AddDocumentAsync(Guid vehicleId, CreateVehicleDocumentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<VehicleTypeDto>>> GetTypesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleTypeDto>> CreateTypeAsync(CreateVehicleTypeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleDto>> AssignDriverAsync(Guid vehicleId, AssignVehicleDriverRequest request, CancellationToken cancellationToken = default);
}
