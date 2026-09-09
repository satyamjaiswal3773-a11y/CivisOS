using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using CivisOS.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public AttendanceService(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ApiResponse<AttendanceDto>> CheckInAsync(
        string userId,
        AttendanceCheckInRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        if (employee is null)
        {
            return ApiResponse<AttendanceDto>.Fail("No employee profile is linked to this user account.");
        }

        if (!employee.IsActive)
        {
            return ApiResponse<AttendanceDto>.Fail("Inactive employees cannot check in.");
        }

        var fence = await _db.GeoFences
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.GeoFenceId, cancellationToken);

        if (fence is null)
        {
            return ApiResponse<AttendanceDto>.Fail("Geo-fence not found.");
        }

        if (fence.Status != GeoFenceStatus.Active)
        {
            return ApiResponse<AttendanceDto>.Fail("Geo-fence is inactive.");
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var alreadyPresent = await _db.Attendances.AnyAsync(
            a => a.EmployeeId == employee.Id
                 && a.AttendanceDate == today
                 && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.CheckedOut),
            cancellationToken);

        if (alreadyPresent)
        {
            return ApiResponse<AttendanceDto>.Fail("You have already checked in for today.");
        }

        var distance = Math.Round(
            GeoMath.DistanceMeters(
                fence.CenterLatitude,
                fence.CenterLongitude,
                request.Latitude,
                request.Longitude),
            2);

        var isInside = GeoMath.IsInsideFence(fence, request.Latitude, request.Longitude);

        var attendance = new Attendance
        {
            EmployeeId = employee.Id,
            GeoFenceId = fence.Id,
            AttendanceDate = today,
            CheckInAtUtc = now,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            CheckInDistanceMeters = distance,
            Status = isInside ? AttendanceStatus.Present : AttendanceStatus.Rejected,
            RejectionReason = isInside
                ? null
                : $"Outside geo-fence '{fence.Name}' (distance {distance} m, radius {fence.RadiusMeters} m)."
        };

        _db.Add(attendance);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await MapByIdAsync(attendance.Id, cancellationToken);
        if (!isInside)
        {
            await _notifications.NotifyUserAsync(
                userId,
                "Attendance rejected",
                attendance.RejectionReason ?? "Check-in rejected: outside geo-fence.",
                NotificationType.AttendanceRejected,
                attendance.Id.ToString(),
                "Attendance",
                cancellationToken);

            return new ApiResponse<AttendanceDto>
            {
                Success = false,
                Message = "Check-in rejected: you are outside the geo-fence.",
                Data = dto,
                Errors = new[] { attendance.RejectionReason! }
            };
        }

        return ApiResponse<AttendanceDto>.Ok(dto!, "Check-in successful.");
    }

    public async Task<ApiResponse<AttendanceDto>> CheckOutAsync(
        string userId,
        AttendanceCheckOutRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        if (employee is null)
        {
            return ApiResponse<AttendanceDto>.Fail("No employee profile is linked to this user account.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var attendance = await _db.Attendances
            .FirstOrDefaultAsync(
                a => a.EmployeeId == employee.Id
                     && a.AttendanceDate == today
                     && a.Status == AttendanceStatus.Present,
                cancellationToken);

        if (attendance is null)
        {
            return ApiResponse<AttendanceDto>.Fail("No open check-in found for today.");
        }

        var fence = await _db.GeoFences
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == attendance.GeoFenceId, cancellationToken);

        if (fence is null)
        {
            return ApiResponse<AttendanceDto>.Fail("Geo-fence linked to check-in was not found.");
        }

        var distance = Math.Round(
            GeoMath.DistanceMeters(
                fence.CenterLatitude,
                fence.CenterLongitude,
                request.Latitude,
                request.Longitude),
            2);

        if (!GeoMath.IsInsideFence(fence, request.Latitude, request.Longitude))
        {
            return ApiResponse<AttendanceDto>.Fail(
                "Check-out rejected: you are outside the geo-fence.",
                new[]
                {
                    $"Outside geo-fence '{fence.Name}' (distance {distance} m, radius {fence.RadiusMeters} m)."
                });
        }

        attendance.CheckOutAtUtc = DateTime.UtcNow;
        attendance.CheckOutLatitude = request.Latitude;
        attendance.CheckOutLongitude = request.Longitude;
        attendance.CheckOutDistanceMeters = distance;
        attendance.Status = AttendanceStatus.CheckedOut;
        attendance.UpdatedAtUtc = DateTime.UtcNow;

        _db.Update(attendance);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await MapByIdAsync(attendance.Id, cancellationToken);
        return ApiResponse<AttendanceDto>.Ok(dto!, "Check-out successful.");
    }

    public async Task<ApiResponse<PagedResult<AttendanceDto>>> GetAttendanceAsync(
        AttendanceListQuery query,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, employeeIdOverride: null, cancellationToken);
    }

    public async Task<ApiResponse<PagedResult<AttendanceDto>>> GetMyAttendanceAsync(
        string userId,
        AttendanceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        if (employee is null)
        {
            return ApiResponse<PagedResult<AttendanceDto>>.Fail(
                "No employee profile is linked to this user account.");
        }

        return await QueryAsync(query, employee.Id, cancellationToken);
    }

    private async Task<ApiResponse<PagedResult<AttendanceDto>>> QueryAsync(
        AttendanceListQuery query,
        Guid? employeeIdOverride,
        CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;
        var employeeId = employeeIdOverride ?? query.EmployeeId;

        var rows = _db.Attendances.AsNoTracking().AsQueryable();

        if (employeeId.HasValue)
        {
            rows = rows.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (query.From.HasValue)
        {
            rows = rows.Where(a => a.AttendanceDate >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            rows = rows.Where(a => a.AttendanceDate <= query.To.Value);
        }

        if (query.Status.HasValue)
        {
            rows = rows.Where(a => a.Status == query.Status.Value);
        }

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderByDescending(a => a.AttendanceDate)
            .ThenByDescending(a => a.CheckInAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                EmployeeCode = a.Employee.EmployeeCode,
                EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
                a.GeoFenceId,
                GeoFenceName = a.GeoFence.Name,
                a.AttendanceDate,
                a.CheckInAtUtc,
                a.CheckOutAtUtc,
                a.CheckInLatitude,
                a.CheckInLongitude,
                a.CheckInDistanceMeters,
                a.CheckOutLatitude,
                a.CheckOutLongitude,
                a.CheckOutDistanceMeters,
                a.Status,
                a.RejectionReason,
                a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = page.Select(a => new AttendanceDto(
            a.Id,
            a.EmployeeId,
            a.EmployeeCode,
            a.EmployeeName,
            a.GeoFenceId,
            a.GeoFenceName,
            a.AttendanceDate,
            a.CheckInAtUtc,
            a.CheckOutAtUtc,
            a.CheckInLatitude,
            a.CheckInLongitude,
            a.CheckInDistanceMeters,
            a.CheckOutLatitude,
            a.CheckOutLongitude,
            a.CheckOutDistanceMeters,
            a.Status,
            a.RejectionReason,
            a.CreatedAtUtc)).ToList();

        return ApiResponse<PagedResult<AttendanceDto>>.Ok(
            PagedResult<AttendanceDto>.Create(items, total, pageNumber, pageSize));
    }

    private async Task<AttendanceDto?> MapByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Attendances
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AttendanceDto(
                a.Id,
                a.EmployeeId,
                a.Employee.EmployeeCode,
                a.Employee.FirstName + " " + a.Employee.LastName,
                a.GeoFenceId,
                a.GeoFence.Name,
                a.AttendanceDate,
                a.CheckInAtUtc,
                a.CheckOutAtUtc,
                a.CheckInLatitude,
                a.CheckInLongitude,
                a.CheckInDistanceMeters,
                a.CheckOutLatitude,
                a.CheckOutLongitude,
                a.CheckOutDistanceMeters,
                a.Status,
                a.RejectionReason,
                a.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
