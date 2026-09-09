using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Employees.DTOs;
using CivisOS.Application.Vehicles.DTOs;
using CivisOS.Application.Vehicles.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using CivisOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public VehicleService(IApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<ApiResponse<PagedResult<VehicleDto>>> GetVehiclesAsync(VehicleListQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var vehicles = _db.Vehicles.AsNoTracking().Include(v => v.VehicleType).Include(v => v.Documents).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            vehicles = vehicles.Where(v =>
                v.Number.ToLower().Contains(term) ||
                v.Make.ToLower().Contains(term) ||
                v.Model.ToLower().Contains(term) ||
                (v.Department != null && v.Department.ToLower().Contains(term)));
        }

        if (query.Status.HasValue)
        {
            vehicles = vehicles.Where(v => v.Status == query.Status.Value);
        }

        if (query.VehicleTypeId.HasValue)
        {
            vehicles = vehicles.Where(v => v.VehicleTypeId == query.VehicleTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            vehicles = vehicles.Where(v => v.Department == query.Department);
        }

        var total = await vehicles.CountAsync(cancellationToken);
        var items = await vehicles
            .OrderByDescending(v => v.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = PagedResult<VehicleDto>.Create(
            items.Select(MapVehicle).ToList(),
            total,
            pageNumber,
            pageSize);

        return ApiResponse<PagedResult<VehicleDto>>.Ok(result);
    }

    public async Task<ApiResponse<VehicleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return vehicle is null
            ? ApiResponse<VehicleDto>.Fail("Vehicle not found.")
            : ApiResponse<VehicleDto>.Ok(MapVehicle(vehicle));
    }

    public async Task<ApiResponse<VehicleDto>> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var typeExists = await _db.VehicleTypes.AnyAsync(t => t.Id == request.VehicleTypeId, cancellationToken);
        if (!typeExists)
        {
            return ApiResponse<VehicleDto>.Fail("Vehicle type not found.");
        }

        var numberTaken = await _db.Vehicles.AnyAsync(v => v.Number == request.Number, cancellationToken);
        if (numberTaken)
        {
            return ApiResponse<VehicleDto>.Fail("A vehicle with this number already exists.");
        }

        var vehicle = new Vehicle
        {
            Number = request.Number.Trim().ToUpperInvariant(),
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            Color = request.Color,
            Status = request.Status,
            Department = request.Department,
            VehicleTypeId = request.VehicleTypeId,
            FuelType = request.FuelType,
            FuelCapacityLiters = request.FuelCapacityLiters,
            CurrentFuelLevelLiters = request.CurrentFuelLevelLiters,
            AverageMileageKmPerLiter = request.AverageMileageKmPerLiter,
            LastServiceDate = request.LastServiceDate,
            NextServiceDueDate = request.NextServiceDueDate,
            OdometerKm = request.OdometerKm,
            MaintenanceNotes = request.MaintenanceNotes
        };

        _db.Add(vehicle);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<ApiResponse<VehicleDto>> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vehicle is null)
        {
            return ApiResponse<VehicleDto>.Fail("Vehicle not found.");
        }

        var typeExists = await _db.VehicleTypes.AnyAsync(t => t.Id == request.VehicleTypeId, cancellationToken);
        if (!typeExists)
        {
            return ApiResponse<VehicleDto>.Fail("Vehicle type not found.");
        }

        var numberTaken = await _db.Vehicles.AnyAsync(
            v => v.Number == request.Number && v.Id != id,
            cancellationToken);
        if (numberTaken)
        {
            return ApiResponse<VehicleDto>.Fail("A vehicle with this number already exists.");
        }

        vehicle.Number = request.Number.Trim().ToUpperInvariant();
        vehicle.Make = request.Make.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.Color = request.Color;
        vehicle.Status = request.Status;
        vehicle.Department = request.Department;
        vehicle.VehicleTypeId = request.VehicleTypeId;
        vehicle.FuelType = request.FuelType;
        vehicle.FuelCapacityLiters = request.FuelCapacityLiters;
        vehicle.CurrentFuelLevelLiters = request.CurrentFuelLevelLiters;
        vehicle.AverageMileageKmPerLiter = request.AverageMileageKmPerLiter;
        vehicle.LastServiceDate = request.LastServiceDate;
        vehicle.NextServiceDueDate = request.NextServiceDueDate;
        vehicle.OdometerKm = request.OdometerKm;
        vehicle.MaintenanceNotes = request.MaintenanceNotes;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        _db.Update(vehicle);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vehicle is null)
        {
            return ApiResponse.Fail("Vehicle not found.");
        }

        _db.Remove(vehicle);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Vehicle deleted.");
    }

    public async Task<ApiResponse<VehicleSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = _db.Vehicles.AsNoTracking();
        var soon = DateTime.UtcNow.AddDays(30);

        var summary = new VehicleSummaryDto(
            await vehicles.CountAsync(cancellationToken),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Available, cancellationToken),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Active, cancellationToken),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Workshop, cancellationToken),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.NonOperational, cancellationToken),
            await _db.VehicleDocuments.AsNoTracking()
                .CountAsync(d => d.ExpiresOn != null && d.ExpiresOn <= soon, cancellationToken));

        return ApiResponse<VehicleSummaryDto>.Ok(summary);
    }

    public async Task<ApiResponse<VehicleDocumentDto>> AddDocumentAsync(
        Guid vehicleId,
        CreateVehicleDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, cancellationToken);
        if (!exists)
        {
            return ApiResponse<VehicleDocumentDto>.Fail("Vehicle not found.");
        }

        var document = new VehicleDocument
        {
            VehicleId = vehicleId,
            DocumentType = request.DocumentType,
            Title = request.Title.Trim(),
            DocumentNumber = request.DocumentNumber,
            IssuedOn = request.IssuedOn,
            ExpiresOn = request.ExpiresOn,
            FilePath = request.FilePath,
            Notes = request.Notes
        };

        _db.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<VehicleDocumentDto>.Ok(MapDocument(document), "Document added.");
    }

    public async Task<ApiResponse<IReadOnlyList<VehicleTypeDto>>> GetTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _db.VehicleTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new VehicleTypeDto(t.Id, t.Name, t.Description, t.IsActive))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<VehicleTypeDto>>.Ok(types);
    }

    public async Task<ApiResponse<VehicleTypeDto>> CreateTypeAsync(CreateVehicleTypeRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _db.VehicleTypes.AnyAsync(t => t.Name == request.Name, cancellationToken);
        if (exists)
        {
            return ApiResponse<VehicleTypeDto>.Fail("Vehicle type already exists.");
        }

        var type = new VehicleType
        {
            Name = request.Name.Trim(),
            Description = request.Description
        };

        _db.Add(type);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<VehicleTypeDto>.Ok(
            new VehicleTypeDto(type.Id, type.Name, type.Description, type.IsActive),
            "Vehicle type created.");
    }

    public async Task<ApiResponse<VehicleDto>> AssignDriverAsync(
        Guid vehicleId,
        AssignVehicleDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);

        if (vehicle is null)
        {
            return ApiResponse<VehicleDto>.Fail("Vehicle not found.");
        }

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return ApiResponse<VehicleDto>.Fail("Employee not found or inactive.");
        }

        if (string.IsNullOrWhiteSpace(employee.UserId))
        {
            return ApiResponse<VehicleDto>.Fail("Employee must be linked to a user account before assignment as driver.");
        }

        var user = await _userManager.FindByIdAsync(employee.UserId);
        if (user is null || !user.IsActive)
        {
            return ApiResponse<VehicleDto>.Fail("Linked user account not found or inactive.");
        }

        if (!await _userManager.IsInRoleAsync(user, AppRoles.Driver))
        {
            return ApiResponse<VehicleDto>.Fail("Employee's user account must have the Driver role.");
        }

        vehicle.DriverEmployeeId = employee.Id;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(vehicle);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(vehicle.Id, cancellationToken);
    }

    private static VehicleDto MapVehicle(Vehicle v) => new(
        v.Id,
        v.Number,
        v.Make,
        v.Model,
        v.Year,
        v.Color,
        v.Status,
        v.Department,
        v.VehicleTypeId,
        v.VehicleType?.Name ?? string.Empty,
        v.FuelType,
        v.FuelCapacityLiters,
        v.CurrentFuelLevelLiters,
        v.AverageMileageKmPerLiter,
        v.LastServiceDate,
        v.NextServiceDueDate,
        v.OdometerKm,
        v.MaintenanceNotes,
        v.DriverEmployeeId,
        v.CreatedAtUtc,
        v.Documents.Select(MapDocument).ToList());

    private static VehicleDocumentDto MapDocument(VehicleDocument d) => new(
        d.Id,
        d.VehicleId,
        d.DocumentType,
        d.Title,
        d.DocumentNumber,
        d.IssuedOn,
        d.ExpiresOn,
        d.FilePath,
        d.Notes);
}
