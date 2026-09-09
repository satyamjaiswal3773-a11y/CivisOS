using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class VehicleDocument : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public VehicleDocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateTime? IssuedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? FilePath { get; set; }
    public string? Notes { get; set; }
}
