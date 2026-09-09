using CivisOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivisOS.Infrastructure.Persistence.Configurations;

public class VehicleTypeConfiguration : IEntityTypeConfiguration<VehicleType>
{
    public void Configure(EntityTypeBuilder<VehicleType> builder)
    {
        builder.ToTable("VehicleTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Make).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Model).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Color).HasMaxLength(50);
        builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.FuelType).HasMaxLength(50);
        builder.Property(x => x.MaintenanceNotes).HasMaxLength(1000);
        builder.Property(x => x.FuelCapacityLiters).HasPrecision(10, 2);
        builder.Property(x => x.CurrentFuelLevelLiters).HasPrecision(10, 2);
        builder.Property(x => x.AverageMileageKmPerLiter).HasPrecision(10, 2);
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.VehicleType)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.DriverEmployeeId);
    }
}

public class VehicleDocumentConfiguration : IEntityTypeConfiguration<VehicleDocument>
{
    public void Configure(EntityTypeBuilder<VehicleDocument> builder)
    {
        builder.ToTable("VehicleDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DocumentNumber).HasMaxLength(100);
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Vehicle)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Token).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.ToTable("Designations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.HasIndex(x => x.EmployeeCode).IsUnique();
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        builder.HasIndex(x => x.DepartmentId);
        builder.HasIndex(x => x.DesignationId);
        builder.HasIndex(x => x.Shift);

        builder.HasOne(x => x.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Designation)
            .WithMany(d => d.Employees)
            .HasForeignKey(x => x.DesignationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supervisor)
            .WithMany(s => s.Subordinates)
            .HasForeignKey(x => x.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GeoFenceConfiguration : IEntityTypeConfiguration<GeoFence>
{
    public void Configure(EntityTypeBuilder<GeoFence> builder)
    {
        builder.ToTable("GeoFences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CenterLatitude).IsRequired();
        builder.Property(x => x.CenterLongitude).IsRequired();
        builder.Property(x => x.RadiusMeters).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.GeoFenceId);
        builder.HasIndex(x => x.AttendanceDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate });

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GeoFence)
            .WithMany()
            .HasForeignKey(x => x.GeoFenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VehicleLocationConfiguration : IEntityTypeConfiguration<VehicleLocation>
{
    public void Configure(EntityTypeBuilder<VehicleLocation> builder)
    {
        builder.ToTable("VehicleLocations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.VehicleId).IsUnique();
        builder.HasIndex(x => x.RecordedAtUtc);
        builder.Property(x => x.SpeedKmh).HasPrecision(8, 2);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VehicleLocationHistoryConfiguration : IEntityTypeConfiguration<VehicleLocationHistory>
{
    public void Configure(EntityTypeBuilder<VehicleLocationHistory> builder)
    {
        builder.ToTable("VehicleLocationHistories");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.RecordedAtUtc);
        builder.HasIndex(x => new { x.VehicleId, x.RecordedAtUtc });
        builder.Property(x => x.SpeedKmh).HasPrecision(8, 2);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VehicleGeoFenceEventConfiguration : IEntityTypeConfiguration<VehicleGeoFenceEvent>
{
    public void Configure(EntityTypeBuilder<VehicleGeoFenceEvent> builder)
    {
        builder.ToTable("VehicleGeoFenceEvents");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.GeoFenceId);
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => x.EventType);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.GeoFence)
            .WithMany()
            .HasForeignKey(x => x.GeoFenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CleaningAreaConfiguration : IEntityTypeConfiguration<CleaningArea>
{
    public void Configure(EntityTypeBuilder<CleaningArea> builder)
    {
        builder.ToTable("CleaningAreas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.GeoFenceId);
        builder.HasIndex(x => x.AssignedEmployeeId);

        builder.HasOne(x => x.GeoFence)
            .WithMany()
            .HasForeignKey(x => x.GeoFenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedEmployee)
            .WithMany()
            .HasForeignKey(x => x.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CleaningScheduleConfiguration : IEntityTypeConfiguration<CleaningSchedule>
{
    public void Configure(EntityTypeBuilder<CleaningSchedule> builder)
    {
        builder.ToTable("CleaningSchedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.CleaningAreaId);
        builder.HasIndex(x => x.AssignedEmployeeId);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.CleaningArea)
            .WithMany(a => a.Schedules)
            .HasForeignKey(x => x.CleaningAreaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AssignedEmployee)
            .WithMany()
            .HasForeignKey(x => x.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CleaningLogConfiguration : IEntityTypeConfiguration<CleaningLog>
{
    public void Configure(EntityTypeBuilder<CleaningLog> builder)
    {
        builder.ToTable("CleaningLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.InspectionNotes).HasMaxLength(1000);
        builder.HasIndex(x => x.CleaningAreaId);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.StartedAtUtc);

        builder.HasOne(x => x.CleaningArea)
            .WithMany(a => a.Logs)
            .HasForeignKey(x => x.CleaningAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CleaningPhotoConfiguration : IEntityTypeConfiguration<CleaningPhoto>
{
    public void Configure(EntityTypeBuilder<CleaningPhoto> builder)
    {
        builder.ToTable("CleaningPhotos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Caption).HasMaxLength(300);
        builder.Property(x => x.AiConfidenceScore).HasPrecision(5, 2);
        builder.HasIndex(x => x.CleaningLogId);
        builder.HasIndex(x => x.PhotoType);

        builder.HasOne(x => x.CleaningLog)
            .WithMany(l => l.Photos)
            .HasForeignKey(x => x.CleaningLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("WorkTasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.StatusNotes).HasMaxLength(1000);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.AssigneeEmployeeId);
        builder.HasIndex(x => x.DeadlineUtc);
        builder.HasIndex(x => x.GeoFenceId);
        builder.HasIndex(x => x.CleaningAreaId);

        builder.HasOne(x => x.AssigneeEmployee)
            .WithMany()
            .HasForeignKey(x => x.AssigneeEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.AssignedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GeoFence)
            .WithMany()
            .HasForeignKey(x => x.GeoFenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CleaningArea)
            .WithMany()
            .HasForeignKey(x => x.CleaningAreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> builder)
    {
        builder.ToTable("AppNotifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.RelatedEntityId).HasMaxLength(100);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(100);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.IsRead);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.Type);
    }
}

public class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.ToTable("UserDeviceTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.DeviceToken).IsRequired().HasMaxLength(512);
        builder.Property(x => x.Platform).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DeviceToken).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450);
        builder.HasIndex(x => x.Type);
    }
}

public class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("ConversationMembers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();

        builder.HasOne(x => x.Conversation)
            .WithMany(c => c.Members)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SenderUserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(4000);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.SentAtUtc);
        builder.HasIndex(x => new { x.ConversationId, x.SentAtUtc });

        builder.HasOne(x => x.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AiAlertConfiguration : IEntityTypeConfiguration<AiAlert>
{
    public void Configure(EntityTypeBuilder<AiAlert> builder)
    {
        builder.ToTable("AiAlerts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.RelatedEntityId).HasMaxLength(100);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(100);
        builder.Property(x => x.AcknowledgedByUserId).HasMaxLength(450);
        builder.HasIndex(x => x.AlertType);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.DetectedAtUtc);
    }
}
