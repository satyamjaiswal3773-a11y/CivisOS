using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class WeeklyOffRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public WeeklyOffPattern Pattern { get; set; } = WeeklyOffPattern.FixedDays;

    /// <summary>Comma-separated DayOfWeek values (0=Sunday..6=Saturday) for FixedDays.</summary>
    public string? FixedDaysCsv { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
}
