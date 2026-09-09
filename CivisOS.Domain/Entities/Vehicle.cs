using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class Vehicle : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Color { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
    public string? Department { get; set; }

    public Guid VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;

    // Fuel
    public string? FuelType { get; set; }
    public decimal? FuelCapacityLiters { get; set; }
    public decimal? CurrentFuelLevelLiters { get; set; }
    public decimal? AverageMileageKmPerLiter { get; set; }

    // Maintenance
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDueDate { get; set; }
    public int? OdometerKm { get; set; }
    public string? MaintenanceNotes { get; set; }

    /// <summary>Employee (Driver role) assigned to this vehicle.</summary>
    public Guid? DriverEmployeeId { get; set; }

    public ICollection<VehicleDocument> Documents { get; set; } = new List<VehicleDocument>();
}
