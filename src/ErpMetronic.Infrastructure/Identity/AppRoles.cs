namespace ErpMetronic.Infrastructure.Identity;

/// <summary>Konstanta nama role agar konsisten dipakai di seluruh aplikasi.</summary>
public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Staff = "Staff";

    public static readonly string[] All = { Administrator, Manager, Staff };
}
