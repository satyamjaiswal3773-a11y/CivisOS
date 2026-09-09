using CivisOS.Domain.Enums;

namespace CivisOS.Application.Vehicles.DTOs;

public record VehicleTypeDto(Guid Id, string Name, string? Description, bool IsActive);

public record CreateVehicleTypeRequest(string Name, string? Description);

public record VehicleDocumentDto(
    Guid Id,
    Guid VehicleId,
    VehicleDocumentType DocumentType,
    string Title,
    string? DocumentNumber,
    DateTime? IssuedOn,
    DateTime? ExpiresOn,
    string? FilePath,
    string? Notes);

public record CreateVehicleDocumentRequest(
    VehicleDocumentType DocumentType,
    string Title,
    string? DocumentNumber,
    DateTime? IssuedOn,
    DateTime? ExpiresOn,
    string? FilePath,
    string? Notes);

public record VehicleDto(
    Guid Id,
    string Number,
    string Make,
    string Model,
    int? Year,
    string? Color,
    VehicleStatus Status,
    string? Department,
    Guid VehicleTypeId,
    string VehicleTypeName,
    string? FuelType,
    decimal? FuelCapacityLiters,
    decimal? CurrentFuelLevelLiters,
    decimal? AverageMileageKmPerLiter,
    DateTime? LastServiceDate,
    DateTime? NextServiceDueDate,
    int? OdometerKm,
    string? MaintenanceNotes,
    Guid? DriverEmployeeId,
    DateTime CreatedAtUtc,
    IReadOnlyList<VehicleDocumentDto> Documents);

public record CreateVehicleRequest(
    string Number,
    string Make,
    string Model,
    int? Year,
    string? Color,
    VehicleStatus Status,
    string? Department,
    Guid VehicleTypeId,
    string? FuelType,
    decimal? FuelCapacityLiters,
    decimal? CurrentFuelLevelLiters,
    decimal? AverageMileageKmPerLiter,
    DateTime? LastServiceDate,
    DateTime? NextServiceDueDate,
    int? OdometerKm,
    string? MaintenanceNotes);

public record UpdateVehicleRequest(
    string Number,
    string Make,
    string Model,
    int? Year,
    string? Color,
    VehicleStatus Status,
    string? Department,
    Guid VehicleTypeId,
    string? FuelType,
    decimal? FuelCapacityLiters,
    decimal? CurrentFuelLevelLiters,
    decimal? AverageMileageKmPerLiter,
    DateTime? LastServiceDate,
    DateTime? NextServiceDueDate,
    int? OdometerKm,
    string? MaintenanceNotes);

public record VehicleSummaryDto(
    int Total,
    int Available,
    int Active,
    int Workshop,
    int NonOperational,
    int DocumentsExpiringSoon);

public record VehicleListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    VehicleStatus? Status = null,
    Guid? VehicleTypeId = null,
    string? Department = null);
