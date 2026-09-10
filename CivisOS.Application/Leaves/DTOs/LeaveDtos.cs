using CivisOS.Domain.Enums;

namespace CivisOS.Application.Leaves.DTOs;

public record LeaveTypeDto(Guid Id, string Code, string Name, bool IsPaid, bool IsActive, string? Description);
public record CreateLeaveTypeRequest(string Code, string Name, bool IsPaid = true, string? Description = null);
public record UpdateLeaveTypeRequest(string Name, bool IsPaid, bool IsActive, string? Description);

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal TotalDays,
    bool IsHalfDay,
    string Reason,
    LeaveRequestStatus Status,
    string? ApproverRemarks,
    DateTime CreatedAtUtc);

public record CreateLeaveRequestDto(
    Guid? EmployeeId,
    Guid LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay = false,
    string Reason = "");

public record LeaveApprovalRequest(string? Remarks = null);

public record LeaveListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    LeaveRequestStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null);
