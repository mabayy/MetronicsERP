namespace ErpMetronic.Infrastructure.Identity;

/// <summary>
/// Klaim hak akses. "Administrator" tidak lagi dikelola sebagai Identity Role,
/// melainkan diturunkan dari Posisi/Jabatan yang ditandai administrator
/// (lihat <see cref="AppClaimsPrincipalFactory"/>). Konstanta ini dipakai sebagai
/// nama klaim role pada [Authorize(Roles = ...)] dan pemeriksaan User.IsInRole.
/// </summary>
public static class AppRoles
{
    public const string Administrator = "Administrator";
}
