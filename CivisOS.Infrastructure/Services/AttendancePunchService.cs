using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendancePunchService : IAttendancePunchService
{
    private readonly IApplicationDbContext _db;
    private readonly IAttendanceProcessor _processor;
    private readonly IAttendanceLockService _lockService;
    private readonly IAttendanceAuditService _audit;

    public AttendancePunchService(
        IApplicationDbContext db,
        IAttendanceProcessor processor,
        IAttendanceLockService lockService,
        IAttendanceAuditService audit)
    {
        _db = db;
        _processor = processor;
        _lockService = lockService;
        _audit = audit;
    }

    public async Task<ApiResponse<AttendancePunchDto>> CreatePunchAsync(
        string actorUserId,
        CreatePunchRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.IsActive, cancellationToken);
        if (employee is null)
            return ApiResponse<AttendancePunchDto>.Fail("Employee not found or inactive.");

        var punchDate = DateOnly.FromDateTime(request.PunchDateTimeUtc);
        if (await _lockService.IsMonthLockedAsync(punchDate.Year, punchDate.Month, cancellationToken))
            return ApiResponse<AttendancePunchDto>.Fail("Attendance is locked for this month.");

        var punch = new AttendancePunch
        {
            EmployeeId = request.EmployeeId,
            PunchDateTimeUtc = DateTime.SpecifyKind(request.PunchDateTimeUtc, DateTimeKind.Utc),
            PunchType = request.PunchType,
            Source = request.Source,
            DeviceId = request.DeviceId,
            IpAddress = request.IpAddress ?? ipAddress,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            CreatedByUserId = actorUserId,
            Remarks = request.Remarks
        };

        _db.Add(punch);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AttendanceAuditAction.PunchCreated,
            actorUserId,
            request.EmployeeId,
            punchDate,
            null,
            $"{request.PunchType} @ {punch.PunchDateTimeUtc:u}",
            request.Remarks,
            punch.IpAddress,
            "AttendancePunch",
            punch.Id,
            cancellationToken);

        await _processor.ProcessEmployeeDayAsync(request.EmployeeId, punchDate, cancellationToken);

        var dto = await MapPunchAsync(punch.Id, cancellationToken);
        return ApiResponse<AttendancePunchDto>.Ok(dto!, "Punch created.");
    }

    public async Task<ApiResponse<AttendancePunchDto>> SelfPunchAsync(
        string userId,
        SelfPunchRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (employee is null)
            return ApiResponse<AttendancePunchDto>.Fail("Employee profile not found for current user.");

        return await CreatePunchAsync(
            userId,
            new CreatePunchRequest(
                employee.Id,
                DateTime.UtcNow,
                request.PunchType,
                request.Source,
                request.DeviceId,
                ipAddress,
                request.Latitude,
                request.Longitude,
                request.AccuracyMeters,
                request.Remarks),
            ipAddress,
            cancellationToken);
    }

    public async Task<ApiResponse<PagedResult<AttendancePunchDto>>> GetPunchesAsync(
        PunchListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q =
            from p in _db.AttendancePunches.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on p.EmployeeId equals e.Id
            select new { p, e };

        if (query.EmployeeId.HasValue)
            q = q.Where(x => x.p.EmployeeId == query.EmployeeId);
        if (query.From.HasValue)
        {
            var fromUtc = query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            q = q.Where(x => x.p.PunchDateTimeUtc >= fromUtc);
        }
        if (query.To.HasValue)
        {
            var toUtc = query.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            q = q.Where(x => x.p.PunchDateTimeUtc <= toUtc);
        }
        if (query.Source.HasValue)
            q = q.Where(x => x.p.Source == query.Source);
        if (query.PunchType.HasValue)
            q = q.Where(x => x.p.PunchType == query.PunchType);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.p.PunchDateTimeUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AttendancePunchDto(
                x.p.Id, x.p.EmployeeId, x.e.EmployeeCode, x.e.FirstName + " " + x.e.LastName,
                x.p.PunchDateTimeUtc, x.p.PunchType, x.p.Source,
                x.p.DeviceId, x.p.IpAddress, x.p.Latitude, x.p.Longitude, x.p.AccuracyMeters,
                x.p.Remarks, x.p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<AttendancePunchDto>>.Ok(
            PagedResult<AttendancePunchDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<IReadOnlyList<AttendancePunchDto>>> GetPunchesForDayAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var items = await (
            from p in _db.AttendancePunches.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on p.EmployeeId equals e.Id
            where p.EmployeeId == employeeId && p.PunchDateTimeUtc >= start && p.PunchDateTimeUtc <= end
            orderby p.PunchDateTimeUtc
            select new AttendancePunchDto(
                p.Id, p.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                p.PunchDateTimeUtc, p.PunchType, p.Source,
                p.DeviceId, p.IpAddress, p.Latitude, p.Longitude, p.AccuracyMeters,
                p.Remarks, p.CreatedAtUtc)
        ).ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<AttendancePunchDto>>.Ok(items);
    }

    private async Task<AttendancePunchDto?> MapPunchAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from p in _db.AttendancePunches.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on p.EmployeeId equals e.Id
            where p.Id == id
            select new AttendancePunchDto(
                p.Id, p.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                p.PunchDateTimeUtc, p.PunchType, p.Source,
                p.DeviceId, p.IpAddress, p.Latitude, p.Longitude, p.AccuracyMeters,
                p.Remarks, p.CreatedAtUtc)
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
