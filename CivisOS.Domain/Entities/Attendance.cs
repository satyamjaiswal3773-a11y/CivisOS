using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class Attendance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid GeoFenceId { get; set; }
    public GeoFence GeoFence { get; set; } = null!;

    /// <summary>Calendar date of the attendance attempt (UTC date component).</summary>
    public DateOnly AttendanceDate { get; set; }

    public DateTime? CheckInAtUtc { get; set; }
    public DateTime? CheckOutAtUtc { get; set; }

    public double CheckInLatitude { get; set; }
    public double CheckInLongitude { get; set; }
    public double CheckInDistanceMeters { get; set; }

    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public double? CheckOutDistanceMeters { get; set; }

    public AttendanceStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}
