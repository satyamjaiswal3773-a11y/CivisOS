using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class UserDeviceToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "web";
    public bool IsActive { get; set; } = true;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
}
