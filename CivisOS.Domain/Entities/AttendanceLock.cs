using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class AttendanceLock : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public AttendanceLockStatus Status { get; set; } = AttendanceLockStatus.Open;
    public string? FinalizedByUserId { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public string? LockedByUserId { get; set; }
    public DateTime? LockedAtUtc { get; set; }
    public string? UnlockedByUserId { get; set; }
    public DateTime? UnlockedAtUtc { get; set; }
    public string? UnlockReason { get; set; }
    public string? Remarks { get; set; }
}
