using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class CleaningPhoto : BaseEntity
{
    public Guid CleaningLogId { get; set; }
    public CleaningLog CleaningLog { get; set; } = null!;
    public CleaningPhotoType PhotoType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public double? AiConfidenceScore { get; set; }
}
