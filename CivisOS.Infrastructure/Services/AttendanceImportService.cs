using System.Globalization;
using System.Text;
using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceImportService : IAttendanceImportService
{
    public const string ValidRowMarker = "VALID_ROW";

    private readonly IApplicationDbContext _db;
    private readonly IAttendanceProcessor _processor;
    private readonly IAttendanceAuditService _audit;

    public AttendanceImportService(
        IApplicationDbContext db,
        IAttendanceProcessor processor,
        IAttendanceAuditService audit)
    {
        _db = db;
        _processor = processor;
        _audit = audit;
    }

    public async Task<ApiResponse<ImportBatchDto>> UploadAndValidateAsync(
        string userId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<ImportBatchDto>.Fail("Only CSV files are supported.");

        string content;
        using (var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            content = await reader.ReadToEndAsync(cancellationToken);

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return ApiResponse<ImportBatchDto>.Fail("CSV file is empty.");

        var startIndex = 0;
        if (IsHeader(lines[0])) startIndex = 1;

        var batch = new AttendanceImportBatch
        {
            FileName = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "text/csv" : contentType,
            UploadedByUserId = userId,
            Status = AttendanceImportBatchStatus.Uploaded
        };
        _db.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        var employeeCodes = await _db.Employees.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.EmployeeCode })
            .ToListAsync(cancellationToken);
        var codeMap = employeeCodes
            .GroupBy(x => x.EmployeeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var total = 0;
        var valid = 0;
        var invalid = 0;
        var duplicate = 0;

        for (var i = startIndex; i < lines.Length; i++)
        {
            var rowNumber = i + 1;
            var raw = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(raw)) continue;
            total++;

            var parts = SplitCsvLine(raw);
            if (parts.Length < 3)
            {
                invalid++;
                _db.Add(new AttendanceImportError
                {
                    ImportBatchId = batch.Id,
                    RowNumber = rowNumber,
                    RawData = Truncate(raw, 2000),
                    ErrorMessage = "Expected columns: EmployeeCode,PunchDateTimeUtc,PunchType."
                });
                continue;
            }

            var empCode = parts[0].Trim();
            var dtRaw = parts[1].Trim();
            var typeRaw = parts[2].Trim();

            if (!codeMap.TryGetValue(empCode, out var employeeId))
            {
                invalid++;
                _db.Add(new AttendanceImportError
                {
                    ImportBatchId = batch.Id,
                    RowNumber = rowNumber,
                    RawData = Truncate(raw, 2000),
                    ErrorMessage = $"Unknown employee code '{empCode}'."
                });
                continue;
            }

            if (!DateTime.TryParse(dtRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var punchUtc)
                && !DateTime.TryParse(dtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out punchUtc))
            {
                invalid++;
                _db.Add(new AttendanceImportError
                {
                    ImportBatchId = batch.Id,
                    RowNumber = rowNumber,
                    RawData = Truncate(raw, 2000),
                    ErrorMessage = "Invalid PunchDateTimeUtc."
                });
                continue;
            }

            punchUtc = DateTime.SpecifyKind(punchUtc, DateTimeKind.Utc);

            if (!TryParsePunchType(typeRaw, out var punchType))
            {
                invalid++;
                _db.Add(new AttendanceImportError
                {
                    ImportBatchId = batch.Id,
                    RowNumber = rowNumber,
                    RawData = Truncate(raw, 2000),
                    ErrorMessage = "PunchType must be In or Out."
                });
                continue;
            }

            var key = $"{empCode}|{punchUtc:O}|{punchType}";
            if (!seenInFile.Add(key))
            {
                duplicate++;
                _db.Add(new AttendanceImportError
                {
                    ImportBatchId = batch.Id,
                    RowNumber = rowNumber,
                    RawData = Truncate(raw, 2000),
                    ErrorMessage = "Duplicate punch in file."
                });
                continue;
            }

            var exists = await _db.AttendancePunches.AsNoTracking().AnyAsync(x =>
                x.EmployeeId == employeeId
                && x.PunchDateTimeUtc == punchUtc
                && x.PunchType == punchType, cancellationToken);
            if (exists)
            {
                duplicate++;
                _db.Add(new AttendanceImportError
                {
                    ImportBatchId = batch.Id,
                    RowNumber = rowNumber,
                    RawData = Truncate(raw, 2000),
                    ErrorMessage = "Duplicate punch already exists."
                });
                continue;
            }

            valid++;
            _db.Add(new AttendanceImportError
            {
                ImportBatchId = batch.Id,
                RowNumber = rowNumber,
                RawData = Truncate($"{empCode}|{punchUtc:O}|{punchType}", 2000),
                ErrorMessage = ValidRowMarker
            });
        }

        batch.TotalRecords = total;
        batch.ValidRecords = valid;
        batch.InvalidRecords = invalid;
        batch.DuplicateRecords = duplicate;
        batch.Status = AttendanceImportBatchStatus.Validated;
        batch.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(batch);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<ImportBatchDto>.Ok(await MapBatchAsync(batch.Id, cancellationToken)!, "CSV validated.");
    }

    public async Task<ApiResponse<ImportBatchDto>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var dto = await MapBatchAsync(batchId, cancellationToken);
        return dto is null
            ? ApiResponse<ImportBatchDto>.Fail("Import batch not found.")
            : ApiResponse<ImportBatchDto>.Ok(dto);
    }

    public async Task<ApiResponse<ImportConfirmResultDto>> ConfirmAsync(
        string userId,
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.AttendanceImportBatches.FirstOrDefaultAsync(x => x.Id == batchId, cancellationToken);
        if (batch is null)
            return ApiResponse<ImportConfirmResultDto>.Fail("Import batch not found.");

        if (batch.Status is AttendanceImportBatchStatus.Confirmed)
            return ApiResponse<ImportConfirmResultDto>.Fail("Batch already confirmed.");
        if (batch.Status is AttendanceImportBatchStatus.Cancelled or AttendanceImportBatchStatus.Failed)
            return ApiResponse<ImportConfirmResultDto>.Fail("Batch cannot be confirmed.");
        if (batch.Status != AttendanceImportBatchStatus.Validated)
            return ApiResponse<ImportConfirmResultDto>.Fail("Batch must be validated before confirm.");

        var validRows = await _db.AttendanceImportErrors
            .Where(x => x.ImportBatchId == batchId && x.ErrorMessage == ValidRowMarker)
            .OrderBy(x => x.RowNumber)
            .ToListAsync(cancellationToken);

        var employeeCodes = await _db.Employees.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.EmployeeCode })
            .ToListAsync(cancellationToken);
        var codeMap = employeeCodes
            .GroupBy(x => x.EmployeeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var punchesInserted = 0;
        var processKeys = new HashSet<(Guid EmployeeId, DateOnly Date)>();

        await _db.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var row in validRows)
            {
                if (string.IsNullOrWhiteSpace(row.RawData)) continue;
                var parts = row.RawData.Split('|');
                if (parts.Length < 3) continue;

                if (!codeMap.TryGetValue(parts[0], out var employeeId)) continue;
                if (!DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var punchUtc))
                    continue;
                if (!TryParsePunchType(parts[2], out var punchType)) continue;

                punchUtc = DateTime.SpecifyKind(punchUtc, DateTimeKind.Utc);

                var exists = await _db.AttendancePunches.AsNoTracking().AnyAsync(x =>
                    x.EmployeeId == employeeId
                    && x.PunchDateTimeUtc == punchUtc
                    && x.PunchType == punchType, ct);
                if (exists) continue;

                _db.Add(new AttendancePunch
                {
                    EmployeeId = employeeId,
                    PunchDateTimeUtc = punchUtc,
                    PunchType = punchType,
                    Source = PunchSource.Import,
                    ImportBatchId = batchId,
                    CreatedByUserId = userId,
                    Remarks = $"Import batch {batchId}"
                });
                punchesInserted++;
                processKeys.Add((employeeId, DateOnly.FromDateTime(punchUtc)));
                _db.Remove(row);
            }

            batch.Status = AttendanceImportBatchStatus.Confirmed;
            batch.ConfirmedAtUtc = DateTime.UtcNow;
            batch.UpdatedAtUtc = DateTime.UtcNow;
            _db.Update(batch);
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                AttendanceAuditAction.ImportConfirmed,
                userId,
                null,
                null,
                null,
                $"Batch={batchId},Punches={punchesInserted}",
                null,
                null,
                "AttendanceImportBatch",
                batchId,
                ct);
        }, cancellationToken);

        var daysProcessed = 0;
        foreach (var key in processKeys.OrderBy(x => x.Date).ThenBy(x => x.EmployeeId))
        {
            var result = await _processor.ProcessEmployeeDayAsync(key.EmployeeId, key.Date, cancellationToken);
            if (result.Success) daysProcessed++;
        }

        return ApiResponse<ImportConfirmResultDto>.Ok(new ImportConfirmResultDto(
            batch.Id,
            batch.TotalRecords,
            batch.ValidRecords,
            batch.InvalidRecords,
            batch.DuplicateRecords,
            punchesInserted,
            daysProcessed), "Import confirmed.");
    }

    private async Task<ImportBatchDto?> MapBatchAsync(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _db.AttendanceImportBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (batch is null) return null;

        var errors = await _db.AttendanceImportErrors.AsNoTracking()
            .Where(x => x.ImportBatchId == id && x.ErrorMessage != ValidRowMarker)
            .OrderBy(x => x.RowNumber)
            .Select(x => new ImportErrorDto(x.RowNumber, x.RawData, x.ErrorMessage))
            .ToListAsync(cancellationToken);

        return new ImportBatchDto(
            batch.Id, batch.FileName, batch.ContentType, batch.Status, batch.UploadedByUserId,
            batch.TotalRecords, batch.ValidRecords, batch.InvalidRecords, batch.DuplicateRecords,
            batch.ConfirmedAtUtc, batch.Remarks, batch.CreatedAtUtc, errors);
    }

    private static bool IsHeader(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.Contains("employeecode") && lower.Contains("punch");
    }

    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }
            sb.Append(ch);
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }

    private static bool TryParsePunchType(string value, out PunchType punchType)
    {
        punchType = default;
        if (string.Equals(value, "In", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
        {
            punchType = PunchType.In;
            return true;
        }
        if (string.Equals(value, "Out", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "2", StringComparison.OrdinalIgnoreCase))
        {
            punchType = PunchType.Out;
            return true;
        }
        return Enum.TryParse(value, true, out punchType) && Enum.IsDefined(punchType);
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
