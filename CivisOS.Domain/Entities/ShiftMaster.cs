using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class ShiftMaster : BaseEntity
{
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int GracePeriodMinutes { get; set; } = 15;
    public int MinimumWorkingMinutes { get; set; } = 480;
    public int AllowedBreakMinutes { get; set; } = 60;
    public int LateAfterMinutes { get; set; } = 15;
    public int EarlyLeavingAfterMinutes { get; set; } = 15;
    public bool OvertimeAllowed { get; set; } = true;
    public int OvertimeAfterMinutes { get; set; } = 30;
    public bool IsNightShift { get; set; }
    public bool IsCrossMidnight { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    public ICollection<EmployeeShiftAssignment> Assignments { get; set; } = new List<EmployeeShiftAssignment>();
}
