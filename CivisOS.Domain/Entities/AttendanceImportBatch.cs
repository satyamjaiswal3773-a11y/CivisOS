using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class AttendanceImportBatch : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public AttendanceImportBatchStatus Status { get; set; } = AttendanceImportBatchStatus.Uploaded;
    public string UploadedByUserId { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public int DuplicateRecords { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public string? Remarks { get; set; }

    public ICollection<AttendanceImportError> Errors { get; set; } = new List<AttendanceImportError>();
    public ICollection<AttendancePunch> Punches { get; set; } = new List<AttendancePunch>();
}
