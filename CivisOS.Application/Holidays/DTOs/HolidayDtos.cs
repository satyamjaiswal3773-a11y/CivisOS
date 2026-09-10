using CivisOS.Domain.Enums;

namespace CivisOS.Application.Holidays.DTOs;

public record HolidayDto(
    Guid Id,
    string Name,
    DateOnly HolidayDate,
    bool IsOptional,
    bool IsActive,
    string? Description,
    Guid? DepartmentId,
    string? DepartmentName);

public record CreateHolidayRequest(
    string Name,
    DateOnly HolidayDate,
    bool IsOptional = false,
    string? Description = null,
    Guid? DepartmentId = null);

public record UpdateHolidayRequest(
    string Name,
    DateOnly HolidayDate,
    bool IsOptional,
    bool IsActive,
    string? Description,
    Guid? DepartmentId);

public record HolidayListQuery(
    int PageNumber = 1,
    int PageSize = 50,
    int? Year = null,
    Guid? DepartmentId = null,
    bool? IsActive = null);

public record WeeklyOffRuleDto(
    Guid Id,
    string Name,
    WeeklyOffPattern Pattern,
    string? FixedDaysCsv,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? EmployeeId,
    string? EmployeeName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? Remarks);

public record CreateWeeklyOffRuleRequest(
    string Name,
    WeeklyOffPattern Pattern,
    string? FixedDaysCsv,
    Guid? DepartmentId,
    Guid? EmployeeId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    string? Remarks = null);

public record UpdateWeeklyOffRuleRequest(
    string Name,
    WeeklyOffPattern Pattern,
    string? FixedDaysCsv,
    Guid? DepartmentId,
    Guid? EmployeeId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? Remarks);
