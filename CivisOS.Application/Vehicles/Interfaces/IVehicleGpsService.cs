using CivisOS.Application.Common.Models;
using CivisOS.Application.Vehicles.DTOs;

namespace CivisOS.Application.Vehicles.Interfaces;

public interface IVehicleGpsService
{
    Task<ApiResponse<VehicleLocationDto>> UpdateLocationAsync(Guid vehicleId, UpdateVehicleLocationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<VehicleLocationDto>> GetLocationAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<VehicleLocationHistoryDto>>> GetLocationHistoryAsync(Guid vehicleId, int hours = 24, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<VehicleLocationDto>>> GetLiveLocationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<VehicleGeoFenceEventDto>>> GetGeoFenceEventsAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
