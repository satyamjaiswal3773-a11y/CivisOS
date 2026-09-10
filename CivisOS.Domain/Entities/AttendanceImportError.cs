using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class AttendanceImportError : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public AttendanceImportBatch ImportBatch { get; set; } = null!;

    public int RowNumber { get; set; }
    public string? RawData { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
