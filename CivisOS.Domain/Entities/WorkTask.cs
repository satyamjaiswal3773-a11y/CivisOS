using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class WorkTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkTaskPriority Priority { get; set; } = WorkTaskPriority.Medium;
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Pending;
    public DateTime? DeadlineUtc { get; set; }
    public Guid? AssigneeEmployeeId { get; set; }
    public Employee? AssigneeEmployee { get; set; }
    public Guid? AssignedByEmployeeId { get; set; }
    public Employee? AssignedByEmployee { get; set; }
    public Guid? GeoFenceId { get; set; }
    public GeoFence? GeoFence { get; set; }
    public Guid? CleaningAreaId { get; set; }
    public CleaningArea? CleaningArea { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public string? StatusNotes { get; set; }
}
