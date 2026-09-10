using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceExceptionService : IAttendanceExceptionService
{
    private readonly IApplicationDbContext _db;
    private readonly IAttendanceAuditService _audit;

    public AttendanceExceptionService(IApplicationDbContext db, IAttendanceAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<PagedResult<AttendanceExceptionDto>>> GetAsync(
        ExceptionListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q =
            from ex in _db.AttendanceExceptions.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on ex.EmployeeId equals e.Id
            select new { ex, e };

        if (query.EmployeeId.HasValue) q = q.Where(x => x.ex.EmployeeId == query.EmployeeId);
        if (query.From.HasValue) q = q.Where(x => x.ex.AttendanceDate >= query.From);
        if (query.To.HasValue) q = q.Where(x => x.ex.AttendanceDate <= query.To);
        if (query.ExceptionType.HasValue) q = q.Where(x => x.ex.ExceptionType == query.ExceptionType);
        if (query.Status.HasValue) q = q.Where(x => x.ex.Status == query.Status);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.ex.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AttendanceExceptionDto(
                x.ex.Id, x.ex.EmployeeId, x.e.EmployeeCode, x.e.FirstName + " " + x.e.LastName,
                x.ex.AttendanceDate, x.ex.ExceptionType, x.ex.Status, x.ex.Message, x.ex.Remarks,
                x.ex.ResolvedByUserId, x.ex.ResolvedAtUtc, x.ex.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<AttendanceExceptionDto>>.Ok(
            PagedResult<AttendanceExceptionDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<AttendanceExceptionDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dto = await (
            from ex in _db.AttendanceExceptions.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on ex.EmployeeId equals e.Id
            where ex.Id == id
            select new AttendanceExceptionDto(
                ex.Id, ex.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                ex.AttendanceDate, ex.ExceptionType, ex.Status, ex.Message, ex.Remarks,
                ex.ResolvedByUserId, ex.ResolvedAtUtc, ex.CreatedAtUtc)
        ).FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ApiResponse<AttendanceExceptionDto>.Fail("Exception not found.")
            : ApiResponse<AttendanceExceptionDto>.Ok(dto);
    }

    public async Task<ApiResponse<AttendanceExceptionDto>> ResolveAsync(
        string userId,
        Guid id,
        ResolveExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.AttendanceExceptions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return ApiResponse<AttendanceExceptionDto>.Fail("Exception not found.");

        if (entity.Status != AttendanceExceptionStatus.Open)
            return ApiResponse<AttendanceExceptionDto>.Fail("Only open exceptions can be resolved.");

        if (request.Status is not (AttendanceExceptionStatus.Resolved or AttendanceExceptionStatus.Ignored))
            return ApiResponse<AttendanceExceptionDto>.Fail("Status must be Resolved or Ignored.");

        entity.Status = request.Status;
        entity.Remarks = request.Remarks;
        entity.ResolvedByUserId = userId;
        entity.ResolvedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AttendanceAuditAction.ExceptionResolved,
            userId,
            entity.EmployeeId,
            entity.AttendanceDate,
            AttendanceExceptionStatus.Open.ToString(),
            request.Status.ToString(),
            request.Remarks,
            null,
            "AttendanceException",
            entity.Id,
            cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }
}
