namespace CivisOS.Domain.Enums;

public enum PunchType
{
    In = 1,
    Out = 2
}

public enum PunchSource
{
    Biometric = 1,
    Web = 2,
    Mobile = 3,
    Qr = 4,
    Admin = 5,
    Import = 6,
    Api = 7,
    GeoFence = 8
}

public enum DayAttendanceStatus
{
    Present = 1,
    Absent = 2,
    Leave = 3,
    HalfDay = 4,
    Holiday = 5,
    WeeklyOff = 6,
    Wfh = 7,
    OnDuty = 8,
    MissingPunch = 9,
    Late = 10,
    EarlyLeaving = 11
}

public enum AttendanceApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    SentBack = 4,
    Cancelled = 5
}

public enum AttendanceExceptionType
{
    MissingIn = 1,
    MissingOut = 2,
    DuplicatePunch = 3,
    InvalidPunchSequence = 4,
    LateArrival = 5,
    EarlyLeaving = 6,
    ExcessBreak = 7,
    InvalidShift = 8,
    UnprocessedAttendance = 9,
    UnapprovedOvertime = 10
}

public enum AttendanceExceptionStatus
{
    Open = 1,
    Resolved = 2,
    Ignored = 3
}

public enum WeeklyOffPattern
{
    FixedDays = 1,
    SecondAndFourthSaturday = 2,
    Rotational = 3
}

public enum AttendanceLockStatus
{
    Open = 1,
    Finalized = 2,
    Locked = 3
}

public enum AttendanceImportBatchStatus
{
    Uploaded = 1,
    Validated = 2,
    Confirmed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum LeaveRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum AttendanceAuditAction
{
    PunchCreated = 1,
    AttendanceProcessed = 2,
    AttendanceCorrected = 3,
    RegularizationSubmitted = 4,
    RegularizationApproved = 5,
    RegularizationRejected = 6,
    RegularizationSentBack = 7,
    ShiftAssigned = 8,
    ShiftChanged = 9,
    OvertimeSubmitted = 10,
    OvertimeApproved = 11,
    OvertimeRejected = 12,
    ExceptionResolved = 13,
    MonthFinalized = 14,
    MonthLocked = 15,
    MonthUnlocked = 16,
    ImportConfirmed = 17,
    ManualAttendance = 18
}
