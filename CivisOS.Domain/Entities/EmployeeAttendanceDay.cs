using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

/// <summary>Processed daily attendance derived from punches, leave, holiday, and shift rules.</summary>
public class EmployeeAttendanceDay : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly AttendanceDate { get; set; }

    public Guid? ShiftId { get; set; }
    public ShiftMaster? Shift { get; set; }

    public DateTime? FirstInUtc { get; set; }
    public DateTime? LastOutUtc { get; set; }
    public int TotalPunches { get; set; }
    public int BreakMinutes { get; set; }
    public int WorkingMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyOutMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public int ExcessBreakMinutes { get; set; }

    public DayAttendanceStatus Status { get; set; } = DayAttendanceStatus.Absent;
    public PunchSource? AttendanceSource { get; set; }

    public bool IsLate { get; set; }
    public bool IsEarlyLeaving { get; set; }
    public bool HasMissingPunch { get; set; }
    public bool IsFinalized { get; set; }
    public bool IsManualOverride { get; set; }

    public Guid? LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }

    public Guid? HolidayId { get; set; }
    public HolidayCalendar? Holiday { get; set; }

    public string? Remarks { get; set; }

    public ICollection<AttendanceBreak> Breaks { get; set; } = new List<AttendanceBreak>();
}
