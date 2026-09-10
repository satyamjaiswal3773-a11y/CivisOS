using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Holidays.DTOs;
using CivisOS.Application.Holidays.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class HolidayService : IHolidayService
{
    private readonly IApplicationDbContext _db;

    public HolidayService(IApplicationDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<HolidayDto>>> GetAsync(
        HolidayListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q =
            from h in _db.HolidayCalendars.AsNoTracking()
            join d in _db.Departments.AsNoTracking() on h.DepartmentId equals d.Id into deps
            from d in deps.DefaultIfEmpty()
            select new { h, DepartmentName = d != null ? d.Name : null };

        if (query.Year.HasValue)
            q = q.Where(x => x.h.HolidayDate.Year == query.Year);
        if (query.DepartmentId.HasValue)
            q = q.Where(x => x.h.DepartmentId == null || x.h.DepartmentId == query.DepartmentId);
        if (query.IsActive.HasValue)
            q = q.Where(x => x.h.IsActive == query.IsActive);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(x => x.h.HolidayDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new HolidayDto(
                x.h.Id, x.h.Name, x.h.HolidayDate, x.h.IsOptional, x.h.IsActive,
                x.h.Description, x.h.DepartmentId, x.DepartmentName))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<HolidayDto>>.Ok(
            PagedResult<HolidayDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<HolidayDto>> CreateAsync(
        CreateHolidayRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<HolidayDto>.Fail("Holiday name is required.");

        var entity = new HolidayCalendar
        {
            Name = request.Name.Trim(),
            HolidayDate = request.HolidayDate,
            IsOptional = request.IsOptional,
            Description = request.Description,
            DepartmentId = request.DepartmentId
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<HolidayDto>.Ok(await MapAsync(entity.Id, cancellationToken)!, "Holiday created.");
    }

    public async Task<ApiResponse<HolidayDto>> UpdateAsync(
        Guid id,
        UpdateHolidayRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.HolidayCalendars.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return ApiResponse<HolidayDto>.Fail("Holiday not found.");

        entity.Name = request.Name.Trim();
        entity.HolidayDate = request.HolidayDate;
        entity.IsOptional = request.IsOptional;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description;
        entity.DepartmentId = request.DepartmentId;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<HolidayDto>.Ok(await MapAsync(entity.Id, cancellationToken)!, "Holiday updated.");
    }

    public async Task<ApiResponse> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.HolidayCalendars.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Holiday not found.");

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Holiday deactivated.");
    }

    public async Task<(bool IsHoliday, Guid? HolidayId)> IsHolidayAsync(
        DateOnly date,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var holiday = await _db.HolidayCalendars.AsNoTracking()
            .Where(x => x.IsActive && x.HolidayDate == date
                && (x.DepartmentId == null || x.DepartmentId == departmentId))
            .OrderByDescending(x => x.DepartmentId.HasValue)
            .FirstOrDefaultAsync(cancellationToken);

        return holiday is null ? (false, null) : (true, holiday.Id);
    }

    private async Task<HolidayDto?> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from h in _db.HolidayCalendars.AsNoTracking()
            join d in _db.Departments.AsNoTracking() on h.DepartmentId equals d.Id into deps
            from d in deps.DefaultIfEmpty()
            where h.Id == id
            select new HolidayDto(
                h.Id, h.Name, h.HolidayDate, h.IsOptional, h.IsActive,
                h.Description, h.DepartmentId, d != null ? d.Name : null)
        ).FirstOrDefaultAsync(cancellationToken);
    }
}

public class WeeklyOffService : IWeeklyOffService
{
    private readonly IApplicationDbContext _db;

    public WeeklyOffService(IApplicationDbContext db) => _db = db;

    public async Task<ApiResponse<IReadOnlyList<WeeklyOffRuleDto>>> GetRulesAsync(
        CancellationToken cancellationToken = default)
    {
        var rules = await _db.WeeklyOffRules.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var items = new List<WeeklyOffRuleDto>();
        foreach (var rule in rules)
            items.Add(await MapAsync(rule.Id, cancellationToken) ?? MapRule(rule, null, null));

        return ApiResponse<IReadOnlyList<WeeklyOffRuleDto>>.Ok(items);
    }

    public async Task<ApiResponse<WeeklyOffRuleDto>> CreateAsync(
        CreateWeeklyOffRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<WeeklyOffRuleDto>.Fail("Rule name is required.");

        if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom)
            return ApiResponse<WeeklyOffRuleDto>.Fail("EffectiveTo cannot be earlier than EffectiveFrom.");

        var entity = new WeeklyOffRule
        {
            Name = request.Name.Trim(),
            Pattern = request.Pattern,
            FixedDaysCsv = request.FixedDaysCsv,
            DepartmentId = request.DepartmentId,
            EmployeeId = request.EmployeeId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Remarks = request.Remarks
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<WeeklyOffRuleDto>.Ok(await MapAsync(entity.Id, cancellationToken)!, "Weekly off rule created.");
    }

    public async Task<ApiResponse<WeeklyOffRuleDto>> UpdateAsync(
        Guid id,
        UpdateWeeklyOffRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.WeeklyOffRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return ApiResponse<WeeklyOffRuleDto>.Fail("Weekly off rule not found.");

        entity.Name = request.Name.Trim();
        entity.Pattern = request.Pattern;
        entity.FixedDaysCsv = request.FixedDaysCsv;
        entity.DepartmentId = request.DepartmentId;
        entity.EmployeeId = request.EmployeeId;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.IsActive = request.IsActive;
        entity.Remarks = request.Remarks;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<WeeklyOffRuleDto>.Ok(await MapAsync(entity.Id, cancellationToken)!, "Weekly off rule updated.");
    }

    public async Task<ApiResponse> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.WeeklyOffRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Weekly off rule not found.");

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Weekly off rule deactivated.");
    }

    public async Task<bool> IsWeeklyOffAsync(
        Guid employeeId,
        Guid? departmentId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var rules = await _db.WeeklyOffRules.AsNoTracking()
            .Where(x => x.IsActive
                && x.EffectiveFrom <= date
                && (x.EffectiveTo == null || x.EffectiveTo >= date)
                && (x.EmployeeId == employeeId
                    || (x.EmployeeId == null && x.DepartmentId == departmentId)
                    || (x.EmployeeId == null && x.DepartmentId == null)))
            .ToListAsync(cancellationToken);

        // Prefer employee-specific, then department, then global
        var rule = rules
            .OrderByDescending(x => x.EmployeeId.HasValue)
            .ThenByDescending(x => x.DepartmentId.HasValue)
            .FirstOrDefault();

        if (rule is null) return false;
        return MatchesPattern(rule, date);
    }

    private static bool MatchesPattern(WeeklyOffRule rule, DateOnly date)
    {
        return rule.Pattern switch
        {
            WeeklyOffPattern.SecondAndFourthSaturday => IsSecondOrFourthSaturday(date),
            WeeklyOffPattern.FixedDays or WeeklyOffPattern.Rotational => MatchesFixedDays(rule.FixedDaysCsv, date),
            _ => MatchesFixedDays(rule.FixedDaysCsv, date)
        };
    }

    private static bool MatchesFixedDays(string? fixedDaysCsv, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(fixedDaysCsv)) return false;
        var day = (int)date.DayOfWeek;
        return fixedDaysCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => int.TryParse(part, out var d) && d == day);
    }

    private static bool IsSecondOrFourthSaturday(DateOnly date)
    {
        if (date.DayOfWeek != DayOfWeek.Saturday) return false;
        var occurrence = ((date.Day - 1) / 7) + 1;
        return occurrence is 2 or 4;
    }

    private async Task<WeeklyOffRuleDto?> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from r in _db.WeeklyOffRules.AsNoTracking()
            join d in _db.Departments.AsNoTracking() on r.DepartmentId equals d.Id into deps
            from d in deps.DefaultIfEmpty()
            join e in _db.Employees.AsNoTracking() on r.EmployeeId equals e.Id into emps
            from e in emps.DefaultIfEmpty()
            where r.Id == id
            select new WeeklyOffRuleDto(
                r.Id, r.Name, r.Pattern, r.FixedDaysCsv,
                r.DepartmentId, d != null ? d.Name : null,
                r.EmployeeId, e != null ? e.FirstName + " " + e.LastName : null,
                r.EffectiveFrom, r.EffectiveTo, r.IsActive, r.Remarks)
        ).FirstOrDefaultAsync(cancellationToken);
    }

    private static WeeklyOffRuleDto MapRule(WeeklyOffRule r, string? deptName, string? empName) => new(
        r.Id, r.Name, r.Pattern, r.FixedDaysCsv,
        r.DepartmentId, deptName, r.EmployeeId, empName,
        r.EffectiveFrom, r.EffectiveTo, r.IsActive, r.Remarks);
}
