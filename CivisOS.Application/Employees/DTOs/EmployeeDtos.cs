using CivisOS.Domain.Enums;

namespace CivisOS.Application.Employees.DTOs;

public record DepartmentDto(Guid Id, string Name, string? Description, bool IsActive);

public record CreateDepartmentRequest(string Name, string? Description);

public record DesignationDto(Guid Id, string Name, string? Description, bool IsActive);

public record CreateDesignationRequest(string Name, string? Description);

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    DateTime JoiningDate,
    WorkShift Shift,
    bool IsActive,
    Guid DepartmentId,
    string DepartmentName,
    Guid DesignationId,
    string DesignationName,
    Guid? SupervisorId,
    string? SupervisorName,
    string? UserId,
    DateTime CreatedAtUtc);

public record CreateEmployeeRequest(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    DateTime JoiningDate,
    WorkShift Shift,
    Guid DepartmentId,
    Guid DesignationId,
    Guid? SupervisorId,
    string? UserId,
    bool IsActive = true);

public record UpdateEmployeeRequest(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    DateTime JoiningDate,
    WorkShift Shift,
    Guid DepartmentId,
    Guid DesignationId,
    Guid? SupervisorId,
    string? UserId,
    bool IsActive);

public record AssignSupervisorRequest(Guid? SupervisorId);

public record AssignVehicleDriverRequest(Guid EmployeeId);

public record EmployeeListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? DepartmentId = null,
    Guid? DesignationId = null,
    WorkShift? Shift = null,
    bool? IsActive = null,
    string? UserId = null);
