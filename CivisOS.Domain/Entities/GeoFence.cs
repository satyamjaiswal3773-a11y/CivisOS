using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class GeoFence : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double RadiusMeters { get; set; }
    public GeoFenceStatus Status { get; set; } = GeoFenceStatus.Active;
}
