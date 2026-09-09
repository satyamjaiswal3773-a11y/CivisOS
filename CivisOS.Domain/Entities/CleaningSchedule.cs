using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class CleaningSchedule : BaseEntity
{
    public Guid CleaningAreaId { get; set; }
    public CleaningArea CleaningArea { get; set; } = null!;
    public CleaningFrequency Frequency { get; set; } = CleaningFrequency.Daily;
    public TimeOnly PreferredTimeLocal { get; set; } = new(9, 0);
    public Guid? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
