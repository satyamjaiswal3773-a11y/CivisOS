using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

/// <summary>Short-lived raw GPS points (retained ~24 hours).</summary>
public class VehicleLocationHistory : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? SpeedKmh { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
