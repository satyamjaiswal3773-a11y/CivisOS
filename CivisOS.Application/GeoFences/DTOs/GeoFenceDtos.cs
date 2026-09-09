using CivisOS.Domain.Enums;

namespace CivisOS.Application.GeoFences.DTOs;

public record GeoFenceDto(
    Guid Id,
    string Name,
    string? Description,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusMeters,
    GeoFenceStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record CreateGeoFenceRequest(
    string Name,
    string? Description,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusMeters,
    GeoFenceStatus Status = GeoFenceStatus.Active);

public record UpdateGeoFenceRequest(
    string Name,
    string? Description,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusMeters,
    GeoFenceStatus Status);

public record GeoFenceCheckRequest(
    double Latitude,
    double Longitude,
    Guid GeoFenceId);

public record GeoFenceCheckResultDto(
    Guid GeoFenceId,
    string GeoFenceName,
    double Latitude,
    double Longitude,
    double DistanceMeters,
    double RadiusMeters,
    bool IsInside);

public record GeoFenceListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    GeoFenceStatus? Status = null);
