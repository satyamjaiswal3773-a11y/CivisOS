using CivisOS.Application.Common.Models;
using CivisOS.Application.Holidays.DTOs;

namespace CivisOS.Application.Holidays.Interfaces;

public interface IHolidayService
{
    Task<ApiResponse<PagedResult<HolidayDto>>> GetAsync(HolidayListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<HolidayDto>> CreateAsync(CreateHolidayRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<HolidayDto>> UpdateAsync(Guid id, UpdateHolidayRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(bool IsHoliday, Guid? HolidayId)> IsHolidayAsync(DateOnly date, Guid? departmentId = null, CancellationToken cancellationToken = default);
}

public interface IWeeklyOffService
{
    Task<ApiResponse<IReadOnlyList<WeeklyOffRuleDto>>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<WeeklyOffRuleDto>> CreateAsync(CreateWeeklyOffRuleRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WeeklyOffRuleDto>> UpdateAsync(Guid id, UpdateWeeklyOffRuleRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsWeeklyOffAsync(Guid employeeId, Guid? departmentId, DateOnly date, CancellationToken cancellationToken = default);
}
