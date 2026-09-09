using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Employees.DTOs;
using CivisOS.Application.Employees.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmployeeService(IApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<ApiResponse<IReadOnlyList<DepartmentDto>>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Description, d.IsActive))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(items);
    }

    public async Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await _db.Departments.AnyAsync(d => d.Name == name, cancellationToken))
        {
            return ApiResponse<DepartmentDto>.Fail("Department already exists.");
        }

        var department = new Department
        {
            Name = name,
            Description = request.Description
        };

        _db.Add(department);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<DepartmentDto>.Ok(
            new DepartmentDto(department.Id, department.Name, department.Description, department.IsActive),
            "Department created.");
    }

    public async Task<ApiResponse<IReadOnlyList<DesignationDto>>> GetDesignationsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.Designations
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DesignationDto(d.Id, d.Name, d.Description, d.IsActive))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<DesignationDto>>.Ok(items);
    }

    public async Task<ApiResponse<DesignationDto>> CreateDesignationAsync(CreateDesignationRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await _db.Designations.AnyAsync(d => d.Name == name, cancellationToken))
        {
            return ApiResponse<DesignationDto>.Fail("Designation already exists.");
        }

        var designation = new Designation
        {
            Name = name,
            Description = request.Description
        };

        _db.Add(designation);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<DesignationDto>.Ok(
            new DesignationDto(designation.Id, designation.Name, designation.Description, designation.IsActive),
            "Designation created.");
    }

    public async Task<ApiResponse<PagedResult<EmployeeDto>>> GetEmployeesAsync(EmployeeListQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var employees = _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Supervisor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            employees = employees.Where(e =>
                e.EmployeeCode.ToLower().Contains(term) ||
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                (e.Email != null && e.Email.ToLower().Contains(term)) ||
                (e.PhoneNumber != null && e.PhoneNumber.Contains(term)));
        }

        if (query.DepartmentId.HasValue)
        {
            employees = employees.Where(e => e.DepartmentId == query.DepartmentId.Value);
        }

        if (query.DesignationId.HasValue)
        {
            employees = employees.Where(e => e.DesignationId == query.DesignationId.Value);
        }

        if (query.Shift.HasValue)
        {
            employees = employees.Where(e => e.Shift == query.Shift.Value);
        }

        if (query.IsActive.HasValue)
        {
            employees = employees.Where(e => e.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            employees = employees.Where(e => e.UserId == query.UserId);
        }

        var total = await employees.CountAsync(cancellationToken);
        var items = await employees
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = PagedResult<EmployeeDto>.Create(
            items.Select(MapEmployee).ToList(),
            total,
            pageNumber,
            pageSize);

        return ApiResponse<PagedResult<EmployeeDto>>.Ok(result);
    }

    public async Task<ApiResponse<EmployeeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Supervisor)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return employee is null
            ? ApiResponse<EmployeeDto>.Fail("Employee not found.")
            : ApiResponse<EmployeeDto>.Ok(MapEmployee(employee));
    }

    public async Task<ApiResponse<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateEmployeeRefsAsync(
            request.DepartmentId,
            request.DesignationId,
            request.SupervisorId,
            request.UserId,
            excludeEmployeeId: null,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResponse<EmployeeDto>.Fail(validationError);
        }

        var code = request.EmployeeCode.Trim().ToUpperInvariant();
        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == code, cancellationToken))
        {
            return ApiResponse<EmployeeDto>.Fail("An employee with this code already exists.");
        }

        var employee = new Employee
        {
            EmployeeCode = code,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            PhoneNumber = request.PhoneNumber,
            JoiningDate = request.JoiningDate,
            Shift = request.Shift,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            SupervisorId = request.SupervisorId,
            UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim(),
            IsActive = request.IsActive
        };

        _db.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    public async Task<ApiResponse<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return ApiResponse<EmployeeDto>.Fail("Employee not found.");
        }

        var validationError = await ValidateEmployeeRefsAsync(
            request.DepartmentId,
            request.DesignationId,
            request.SupervisorId,
            request.UserId,
            excludeEmployeeId: id,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResponse<EmployeeDto>.Fail(validationError);
        }

        if (request.SupervisorId == id)
        {
            return ApiResponse<EmployeeDto>.Fail("An employee cannot be their own supervisor.");
        }

        var code = request.EmployeeCode.Trim().ToUpperInvariant();
        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == code && e.Id != id, cancellationToken))
        {
            return ApiResponse<EmployeeDto>.Fail("An employee with this code already exists.");
        }

        employee.EmployeeCode = code;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        employee.PhoneNumber = request.PhoneNumber;
        employee.JoiningDate = request.JoiningDate;
        employee.Shift = request.Shift;
        employee.DepartmentId = request.DepartmentId;
        employee.DesignationId = request.DesignationId;
        employee.SupervisorId = request.SupervisorId;
        employee.UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();
        employee.IsActive = request.IsActive;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        _db.Update(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    public async Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return ApiResponse.Fail("Employee not found.");
        }

        var hasSubordinates = await _db.Employees.AnyAsync(e => e.SupervisorId == id, cancellationToken);
        if (hasSubordinates)
        {
            return ApiResponse.Fail("Cannot delete employee who still has subordinates. Reassign them first.");
        }

        var assignedToVehicle = await _db.Vehicles.AnyAsync(v => v.DriverEmployeeId == id, cancellationToken);
        if (assignedToVehicle)
        {
            return ApiResponse.Fail("Cannot delete employee assigned as a vehicle driver. Unassign them first.");
        }

        _db.Remove(employee);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Employee deleted.");
    }

    public async Task<ApiResponse<EmployeeDto>> AssignSupervisorAsync(
        Guid id,
        AssignSupervisorRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return ApiResponse<EmployeeDto>.Fail("Employee not found.");
        }

        if (request.SupervisorId.HasValue)
        {
            if (request.SupervisorId.Value == id)
            {
                return ApiResponse<EmployeeDto>.Fail("An employee cannot be their own supervisor.");
            }

            var supervisorExists = await _db.Employees.AnyAsync(
                e => e.Id == request.SupervisorId.Value && e.IsActive,
                cancellationToken);

            if (!supervisorExists)
            {
                return ApiResponse<EmployeeDto>.Fail("Supervisor not found or inactive.");
            }
        }

        employee.SupervisorId = request.SupervisorId;
        employee.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<string?> ValidateEmployeeRefsAsync(
        Guid departmentId,
        Guid designationId,
        Guid? supervisorId,
        string? userId,
        Guid? excludeEmployeeId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Departments.AnyAsync(d => d.Id == departmentId && d.IsActive, cancellationToken))
        {
            return "Department not found or inactive.";
        }

        if (!await _db.Designations.AnyAsync(d => d.Id == designationId && d.IsActive, cancellationToken))
        {
            return "Designation not found or inactive.";
        }

        if (supervisorId.HasValue)
        {
            var supervisorQuery = _db.Employees.Where(e => e.Id == supervisorId.Value && e.IsActive);
            if (!await supervisorQuery.AnyAsync(cancellationToken))
            {
                return "Supervisor not found or inactive.";
            }
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await _userManager.FindByIdAsync(userId.Trim());
            if (user is null || !user.IsActive)
            {
                return "Linked user account not found or inactive.";
            }

            var userTaken = await _db.Employees.AnyAsync(
                e => e.UserId == userId.Trim() && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value),
                cancellationToken);

            if (userTaken)
            {
                return "This user account is already linked to another employee.";
            }
        }

        return null;
    }

    private static EmployeeDto MapEmployee(Employee e) => new(
        e.Id,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        e.Email,
        e.PhoneNumber,
        e.JoiningDate,
        e.Shift,
        e.IsActive,
        e.DepartmentId,
        e.Department?.Name ?? string.Empty,
        e.DesignationId,
        e.Designation?.Name ?? string.Empty,
        e.SupervisorId,
        e.Supervisor is null ? null : $"{e.Supervisor.FirstName} {e.Supervisor.LastName}",
        e.UserId,
        e.CreatedAtUtc);
}
