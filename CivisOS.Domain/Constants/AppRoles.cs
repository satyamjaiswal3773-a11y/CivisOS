namespace CivisOS.Domain.Constants;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string SocietyAdmin = "SocietyAdmin";
    public const string Supervisor = "Supervisor";
    public const string Employee = "Employee";
    public const string Driver = "Driver";
    public const string Security = "Security";
    public const string Resident = "Resident";

    public static readonly string[] All =
    [
        SuperAdmin,
        SocietyAdmin,
        Supervisor,
        Employee,
        Driver,
        Security,
        Resident
    ];

    public static readonly string[] AdminRoles =
    [
        SuperAdmin,
        SocietyAdmin
    ];
}
