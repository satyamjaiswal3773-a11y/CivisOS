using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Application.Vehicles.DTOs;
using CivisOS.Application.Vehicles.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using CivisOS.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class VehicleGpsService : IVehicleGpsService
{
    private static readonly TimeSpan HistoryRetention = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public VehicleGpsService(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ApiResponse<VehicleLocationDto>> UpdateLocationAsync(
        Guid vehicleId,
        UpdateVehicleLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);

        if (vehicle is null)
        {
            return ApiResponse<VehicleLocationDto>.Fail("Vehicle not found.");
        }

        var recordedAt = request.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow;
        if (recordedAt > DateTime.UtcNow.AddMinutes(5))
        {
            return ApiResponse<VehicleLocationDto>.Fail("Timestamp cannot be more than 5 minutes in the future.");
        }

        var previous = await _db.VehicleLocations
            .FirstOrDefaultAsync(l => l.VehicleId == vehicleId, cancellationToken);

        var activeFences = await _db.GeoFences
            .AsNoTracking()
            .Where(f => f.Status == GeoFenceStatus.Active)
            .ToListAsync(cancellationToken);

        if (previous is not null)
        {
            foreach (var fence in activeFences)
            {
                var wasInside = GeoMath.IsInsideFence(fence, previous.Latitude, previous.Longitude);
                var isInside = GeoMath.IsInsideFence(fence, request.Latitude, request.Longitude);

                if (wasInside == isInside)
                {
                    continue;
                }

                _db.Add(new VehicleGeoFenceEvent
                {
                    VehicleId = vehicleId,
                    GeoFenceId = fence.Id,
                    EventType = isInside ? VehicleGeoFenceEventType.Entered : VehicleGeoFenceEventType.Exited,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    OccurredAtUtc = recordedAt
                });

                if (!isInside)
                {
                    await NotifyFenceExitAsync(vehicle, fence.Name, cancellationToken);
                }
            }

            previous.Latitude = request.Latitude;
            previous.Longitude = request.Longitude;
            previous.SpeedKmh = request.SpeedKmh;
            previous.RecordedAtUtc = recordedAt;
            previous.UpdatedAtUtc = DateTime.UtcNow;
            _db.Update(previous);
        }
        else
        {
            // First ping: record Entered for fences that contain the point.
            foreach (var fence in activeFences)
            {
                if (!GeoMath.IsInsideFence(fence, request.Latitude, request.Longitude))
                {
                    continue;
                }

                _db.Add(new VehicleGeoFenceEvent
                {
                    VehicleId = vehicleId,
                    GeoFenceId = fence.Id,
                    EventType = VehicleGeoFenceEventType.Entered,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    OccurredAtUtc = recordedAt
                });
            }

            _db.Add(new VehicleLocation
            {
                VehicleId = vehicleId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                SpeedKmh = request.SpeedKmh,
                RecordedAtUtc = recordedAt
            });
        }

        _db.Add(new VehicleLocationHistory
        {
            VehicleId = vehicleId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SpeedKmh = request.SpeedKmh,
            RecordedAtUtc = recordedAt
        });

        var cutoff = DateTime.UtcNow - HistoryRetention;
        var stale = await _db.VehicleLocationHistories
            .Where(h => h.RecordedAtUtc < cutoff)
            .ToListAsync(cancellationToken);
        foreach (var point in stale)
        {
            _db.Remove(point);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var dto = new VehicleLocationDto(
            vehicleId,
            vehicle.Number,
            request.Latitude,
            request.Longitude,
            request.SpeedKmh,
            recordedAt,
            DateTime.UtcNow);

        return ApiResponse<VehicleLocationDto>.Ok(dto, "Vehicle location updated.");
    }

    public async Task<ApiResponse<VehicleLocationDto>> GetLocationAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var location = await _db.VehicleLocations
            .AsNoTracking()
            .Where(l => l.VehicleId == vehicleId)
            .Select(l => new VehicleLocationDto(
                l.VehicleId,
                l.Vehicle.Number,
                l.Latitude,
                l.Longitude,
                l.SpeedKmh,
                l.RecordedAtUtc,
                l.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return location is null
            ? ApiResponse<VehicleLocationDto>.Fail("No location recorded for this vehicle.")
            : ApiResponse<VehicleLocationDto>.Ok(location);
    }

    public async Task<ApiResponse<IReadOnlyList<VehicleLocationHistoryDto>>> GetLocationHistoryAsync(
        Guid vehicleId,
        int hours = 24,
        CancellationToken cancellationToken = default)
    {
        var vehicleExists = await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, cancellationToken);
        if (!vehicleExists)
        {
            return ApiResponse<IReadOnlyList<VehicleLocationHistoryDto>>.Fail("Vehicle not found.");
        }

        hours = hours is < 1 or > 24 ? 24 : hours;
        var since = DateTime.UtcNow.AddHours(-hours);

        var items = await _db.VehicleLocationHistories
            .AsNoTracking()
            .Where(h => h.VehicleId == vehicleId && h.RecordedAtUtc >= since)
            .OrderByDescending(h => h.RecordedAtUtc)
            .Select(h => new VehicleLocationHistoryDto(
                h.Id,
                h.VehicleId,
                h.Latitude,
                h.Longitude,
                h.SpeedKmh,
                h.RecordedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<VehicleLocationHistoryDto>>.Ok(items);
    }

    public async Task<ApiResponse<IReadOnlyList<VehicleLocationDto>>> GetLiveLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _db.VehicleLocations
            .AsNoTracking()
            .OrderBy(l => l.Vehicle.Number)
            .Select(l => new VehicleLocationDto(
                l.VehicleId,
                l.Vehicle.Number,
                l.Latitude,
                l.Longitude,
                l.SpeedKmh,
                l.RecordedAtUtc,
                l.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<VehicleLocationDto>>.Ok(items);
    }

    public async Task<ApiResponse<IReadOnlyList<VehicleGeoFenceEventDto>>> GetGeoFenceEventsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var vehicleExists = await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, cancellationToken);
        if (!vehicleExists)
        {
            return ApiResponse<IReadOnlyList<VehicleGeoFenceEventDto>>.Fail("Vehicle not found.");
        }

        var items = await _db.VehicleGeoFenceEvents
            .AsNoTracking()
            .Where(e => e.VehicleId == vehicleId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Select(e => new VehicleGeoFenceEventDto(
                e.Id,
                e.VehicleId,
                e.Vehicle.Number,
                e.GeoFenceId,
                e.GeoFence.Name,
                e.EventType,
                e.Latitude,
                e.Longitude,
                e.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<VehicleGeoFenceEventDto>>.Ok(items);
    }

    private async Task NotifyFenceExitAsync(
        Vehicle vehicle,
        string fenceName,
        CancellationToken cancellationToken)
    {
        if (vehicle.DriverEmployeeId is null)
        {
            return;
        }

        var driver = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == vehicle.DriverEmployeeId.Value, cancellationToken);
        if (driver?.UserId is null)
        {
            return;
        }

        await _notifications.NotifyUserAsync(
            driver.UserId,
            "Vehicle left geo-fence",
            $"Vehicle {vehicle.Number} exited fence '{fenceName}'.",
            NotificationType.VehicleFenceViolation,
            vehicle.Id.ToString(),
            "Vehicle",
            cancellationToken);
    }
}
