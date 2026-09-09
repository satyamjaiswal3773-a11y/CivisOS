using CivisOS.Domain.Enums;

namespace CivisOS.Application.Vehicles.DTOs;

public record VehicleLocationDto(
    Guid VehicleId,
    string VehicleNumber,
    double Latitude,
    double Longitude,
    double? SpeedKmh,
    DateTime RecordedAtUtc,
    DateTime? UpdatedAtUtc);

public record VehicleLocationHistoryDto(
    Guid Id,
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double? SpeedKmh,
    DateTime RecordedAtUtc);

public record VehicleGeoFenceEventDto(
    Guid Id,
    Guid VehicleId,
    string VehicleNumber,
    Guid GeoFenceId,
    string GeoFenceName,
    VehicleGeoFenceEventType EventType,
    double Latitude,
    double Longitude,
    DateTime OccurredAtUtc);

public record UpdateVehicleLocationRequest(
    double Latitude,
    double Longitude,
    double? SpeedKmh,
    DateTime? Timestamp);
