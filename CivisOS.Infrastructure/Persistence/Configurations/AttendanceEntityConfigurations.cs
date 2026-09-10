using CivisOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivisOS.Infrastructure.Persistence.Configurations;

public class ShiftMasterConfiguration : IEntityTypeConfiguration<ShiftMaster>
{
    public void Configure(EntityTypeBuilder<ShiftMaster> builder)
    {
        builder.ToTable("ShiftMasters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ShiftCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ShiftName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.ShiftCode).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

public class EmployeeShiftAssignmentConfiguration : IEntityTypeConfiguration<EmployeeShiftAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeShiftAssignment> builder)
    {
        builder.ToTable("EmployeeShiftAssignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.AssignedByUserId).HasMaxLength(450);
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom, x.EffectiveTo });
        builder.HasIndex(x => x.ShiftId);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Shift).WithMany(x => x.Assignments).HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendancePunchConfiguration : IEntityTypeConfiguration<AttendancePunch>
{
    public void Configure(EntityTypeBuilder<AttendancePunch> builder)
    {
        builder.ToTable("AttendancePunches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DeviceId).HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.PunchDateTimeUtc);
        builder.HasIndex(x => new { x.EmployeeId, x.PunchDateTimeUtc });
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.ImportBatchId);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ImportBatch).WithMany(x => x.Punches).HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeAttendanceDayConfiguration : IEntityTypeConfiguration<EmployeeAttendanceDay>
{
    public void Configure(EntityTypeBuilder<EmployeeAttendanceDay> builder)
    {
        builder.ToTable("EmployeeAttendanceDays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique();
        builder.HasIndex(x => x.AttendanceDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ShiftId);
        builder.HasIndex(x => x.IsFinalized);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LeaveRequest).WithMany().HasForeignKey(x => x.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Holiday).WithMany().HasForeignKey(x => x.HolidayId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceBreakConfiguration : IEntityTypeConfiguration<AttendanceBreak>
{
    public void Configure(EntityTypeBuilder<AttendanceBreak> builder)
    {
        builder.ToTable("AttendanceBreaks");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EmployeeAttendanceDayId);

        builder.HasOne(x => x.EmployeeAttendanceDay)
            .WithMany(x => x.Breaks)
            .HasForeignKey(x => x.EmployeeAttendanceDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttendanceRegularizationConfiguration : IEntityTypeConfiguration<AttendanceRegularization>
{
    public void Configure(EntityTypeBuilder<AttendanceRegularization> builder)
    {
        builder.ToTable("AttendanceRegularizations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.AttachmentReference).HasMaxLength(500);
        builder.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(450);
        builder.Property(x => x.RejectedByUserId).HasMaxLength(450);
        builder.Property(x => x.ApproverRemarks).HasMaxLength(1000);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.AttendanceDate);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.ToTable("OvertimeRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestedHours).HasPrecision(8, 2);
        builder.Property(x => x.ApprovedHours).HasPrecision(8, 2);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(450);
        builder.Property(x => x.RejectedByUserId).HasMaxLength(450);
        builder.Property(x => x.ApproverRemarks).HasMaxLength(1000);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.OvertimeDate);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceExceptionRecordConfiguration : IEntityTypeConfiguration<AttendanceExceptionRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceExceptionRecord> builder)
    {
        builder.ToTable("AttendanceExceptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.ResolvedByUserId).HasMaxLength(450);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.AttendanceDate);
        builder.HasIndex(x => x.ExceptionType);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.EmployeeAttendanceDay).WithMany().HasForeignKey(x => x.EmployeeAttendanceDayId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceLockConfiguration : IEntityTypeConfiguration<AttendanceLock>
{
    public void Configure(EntityTypeBuilder<AttendanceLock> builder)
    {
        builder.ToTable("AttendanceLocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FinalizedByUserId).HasMaxLength(450);
        builder.Property(x => x.LockedByUserId).HasMaxLength(450);
        builder.Property(x => x.UnlockedByUserId).HasMaxLength(450);
        builder.Property(x => x.UnlockReason).HasMaxLength(500);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Year, x.Month }).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}

public class AttendanceAuditLogConfiguration : IEntityTypeConfiguration<AttendanceAuditLog>
{
    public void Configure(EntityTypeBuilder<AttendanceAuditLog> builder)
    {
        builder.ToTable("AttendanceAuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.OldValue).HasMaxLength(4000);
        builder.Property(x => x.NewValue).HasMaxLength(4000);
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(100);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.AttendanceDate);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

public class AttendanceImportBatchConfiguration : IEntityTypeConfiguration<AttendanceImportBatch>
{
    public void Configure(EntityTypeBuilder<AttendanceImportBatch> builder)
    {
        builder.ToTable("AttendanceImportBatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder.Property(x => x.UploadedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

public class AttendanceImportErrorConfiguration : IEntityTypeConfiguration<AttendanceImportError>
{
    public void Configure(EntityTypeBuilder<AttendanceImportError> builder)
    {
        builder.ToTable("AttendanceImportErrors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RawData).HasMaxLength(2000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => x.ImportBatchId);

        builder.HasOne(x => x.ImportBatch)
            .WithMany(x => x.Errors)
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WeeklyOffRuleConfiguration : IEntityTypeConfiguration<WeeklyOffRule>
{
    public void Configure(EntityTypeBuilder<WeeklyOffRule> builder)
    {
        builder.ToTable("WeeklyOffRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.FixedDaysCsv).HasMaxLength(50);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.DepartmentId);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class HolidayCalendarConfiguration : IEntityTypeConfiguration<HolidayCalendar>
{
    public void Configure(EntityTypeBuilder<HolidayCalendar> builder)
    {
        builder.ToTable("HolidayCalendars");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.HolidayDate);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.HolidayDate, x.DepartmentId });

        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalDays).HasPrecision(8, 2);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(450);
        builder.Property(x => x.RejectedByUserId).HasMaxLength(450);
        builder.Property(x => x.ApproverRemarks).HasMaxLength(1000);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.FromDate);
        builder.HasIndex(x => x.ToDate);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
