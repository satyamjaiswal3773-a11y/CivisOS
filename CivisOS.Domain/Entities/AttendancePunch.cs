using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

/// <summary>Immutable raw punch. Corrections never overwrite these rows.</summary>
public class AttendancePunch : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime PunchDateTimeUtc { get; set; }
    public PunchType PunchType { get; set; }
    public PunchSource Source { get; set; }

    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AccuracyMeters { get; set; }

    public Guid? ImportBatchId { get; set; }
    public AttendanceImportBatch? ImportBatch { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? Remarks { get; set; }
}
