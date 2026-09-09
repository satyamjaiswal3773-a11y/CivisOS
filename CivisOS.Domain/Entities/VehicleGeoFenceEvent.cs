using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class VehicleGeoFenceEvent : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid GeoFenceId { get; set; }
    public GeoFence GeoFence { get; set; } = null!;

    public VehicleGeoFenceEventType EventType { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
