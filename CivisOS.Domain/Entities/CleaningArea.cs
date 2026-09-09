using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class CleaningArea : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? GeoFenceId { get; set; }
    public GeoFence? GeoFence { get; set; }
    public CleaningFrequency Frequency { get; set; } = CleaningFrequency.Daily;
    public Guid? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public DateTime? LastCleanedAtUtc { get; set; }
    public DateTime? NextCleanDueAtUtc { get; set; }
    public CleaningAreaStatus Status { get; set; } = CleaningAreaStatus.Active;

    public ICollection<CleaningSchedule> Schedules { get; set; } = new List<CleaningSchedule>();
    public ICollection<CleaningLog> Logs { get; set; } = new List<CleaningLog>();
}
