using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

/// <summary>Last known GPS position for a vehicle (one row per vehicle).</summary>
public class VehicleLocation : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? SpeedKmh { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
