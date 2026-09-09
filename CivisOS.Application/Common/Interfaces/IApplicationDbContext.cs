using CivisOS.Domain.Entities;

namespace CivisOS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<Vehicle> Vehicles { get; }
    IQueryable<VehicleType> VehicleTypes { get; }
    IQueryable<VehicleDocument> VehicleDocuments { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<Department> Departments { get; }
    IQueryable<Designation> Designations { get; }
    IQueryable<Employee> Employees { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
