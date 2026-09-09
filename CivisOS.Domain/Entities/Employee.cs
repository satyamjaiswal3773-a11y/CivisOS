using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class Employee : BaseEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime JoiningDate { get; set; }
    public WorkShift Shift { get; set; } = WorkShift.General;
    public bool IsActive { get; set; } = true;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public Guid DesignationId { get; set; }
    public Designation Designation { get; set; } = null!;

    /// <summary>Another employee who supervises this one.</summary>
    public Guid? SupervisorId { get; set; }
    public Employee? Supervisor { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    /// <summary>Linked ASP.NET Identity user id (string).</summary>
    public string? UserId { get; set; }
}
