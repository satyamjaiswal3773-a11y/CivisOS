using CivisOS.Application.Common.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<VehicleDocument> VehicleDocuments => Set<VehicleDocument>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<GeoFence> GeoFences => Set<GeoFence>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<VehicleLocation> VehicleLocations => Set<VehicleLocation>();
    public DbSet<VehicleLocationHistory> VehicleLocationHistories => Set<VehicleLocationHistory>();
    public DbSet<VehicleGeoFenceEvent> VehicleGeoFenceEvents => Set<VehicleGeoFenceEvent>();
    public DbSet<CleaningArea> CleaningAreas => Set<CleaningArea>();
    public DbSet<CleaningSchedule> CleaningSchedules => Set<CleaningSchedule>();
    public DbSet<CleaningLog> CleaningLogs => Set<CleaningLog>();
    public DbSet<CleaningPhoto> CleaningPhotos => Set<CleaningPhoto>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<AiAlert> AiAlerts => Set<AiAlert>();

    IQueryable<Vehicle> IApplicationDbContext.Vehicles => Vehicles;
    IQueryable<VehicleType> IApplicationDbContext.VehicleTypes => VehicleTypes;
    IQueryable<VehicleDocument> IApplicationDbContext.VehicleDocuments => VehicleDocuments;
    IQueryable<RefreshToken> IApplicationDbContext.RefreshTokens => RefreshTokens;
    IQueryable<Department> IApplicationDbContext.Departments => Departments;
    IQueryable<Designation> IApplicationDbContext.Designations => Designations;
    IQueryable<Employee> IApplicationDbContext.Employees => Employees;
    IQueryable<GeoFence> IApplicationDbContext.GeoFences => GeoFences;
    IQueryable<Attendance> IApplicationDbContext.Attendances => Attendances;
    IQueryable<VehicleLocation> IApplicationDbContext.VehicleLocations => VehicleLocations;
    IQueryable<VehicleLocationHistory> IApplicationDbContext.VehicleLocationHistories => VehicleLocationHistories;
    IQueryable<VehicleGeoFenceEvent> IApplicationDbContext.VehicleGeoFenceEvents => VehicleGeoFenceEvents;
    IQueryable<CleaningArea> IApplicationDbContext.CleaningAreas => CleaningAreas;
    IQueryable<CleaningSchedule> IApplicationDbContext.CleaningSchedules => CleaningSchedules;
    IQueryable<CleaningLog> IApplicationDbContext.CleaningLogs => CleaningLogs;
    IQueryable<CleaningPhoto> IApplicationDbContext.CleaningPhotos => CleaningPhotos;
    IQueryable<WorkTask> IApplicationDbContext.WorkTasks => WorkTasks;
    IQueryable<AppNotification> IApplicationDbContext.AppNotifications => AppNotifications;
    IQueryable<UserDeviceToken> IApplicationDbContext.UserDeviceTokens => UserDeviceTokens;
    IQueryable<Conversation> IApplicationDbContext.Conversations => Conversations;
    IQueryable<ConversationMember> IApplicationDbContext.ConversationMembers => ConversationMembers;
    IQueryable<Message> IApplicationDbContext.Messages => Messages;
    IQueryable<AiAlert> IApplicationDbContext.AiAlerts => AiAlerts;

    void IApplicationDbContext.Add<TEntity>(TEntity entity) => Set<TEntity>().Add(entity);
    void IApplicationDbContext.Update<TEntity>(TEntity entity) => Set<TEntity>().Update(entity);
    void IApplicationDbContext.Remove<TEntity>(TEntity entity) => Set<TEntity>().Remove(entity);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
