using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class EmployeeShiftAssignment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid ShiftId { get; set; }
    public ShiftMaster Shift { get; set; } = null!;

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
    public string? AssignedByUserId { get; set; }
}
