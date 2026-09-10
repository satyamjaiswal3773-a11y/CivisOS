using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class HolidayCalendar : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly HolidayDate { get; set; }
    public bool IsOptional { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
