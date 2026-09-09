using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class CleaningLog : BaseEntity
{
    public Guid CleaningAreaId { get; set; }
    public CleaningArea CleaningArea { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public CleaningLogStatus Status { get; set; } = CleaningLogStatus.Started;
    public string? Notes { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? InspectionScore { get; set; }
    public string? InspectionNotes { get; set; }

    public ICollection<CleaningPhoto> Photos { get; set; } = new List<CleaningPhoto>();
}
