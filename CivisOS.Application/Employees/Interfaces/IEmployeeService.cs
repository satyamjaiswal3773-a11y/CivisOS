using CivisOS.Application.Common.Models;
using CivisOS.Application.Employees.DTOs;

namespace CivisOS.Application.Employees.Interfaces;

public interface IEmployeeService
{
    Task<ApiResponse<IReadOnlyList<DepartmentDto>>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<DesignationDto>>> GetDesignationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<DesignationDto>> CreateDesignationAsync(CreateDesignationRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<EmployeeDto>>> GetEmployeesAsync(EmployeeListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeDto>> AssignSupervisorAsync(Guid id, AssignSupervisorRequest request, CancellationToken cancellationToken = default);
}
