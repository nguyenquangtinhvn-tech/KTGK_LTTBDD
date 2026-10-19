SELECT
    u.UserName AS TenTaiKhoan,
    u.IsActive AS DangHoatDong,
    r.Name AS TenVaiTro,
    r.DisplayName AS TenVaiTroHienThi
FROM dbo.AbpUsers u
LEFT JOIN dbo.AbpUserRoles ur
    ON ur.UserId = u.Id
   AND (
       ur.TenantId = u.TenantId
       OR (ur.TenantId IS NULL AND u.TenantId IS NULL)
   )
LEFT JOIN dbo.AbpRoles r
    ON r.Id = ur.RoleId
   AND (
       r.TenantId = u.TenantId
       OR (r.TenantId IS NULL AND u.TenantId IS NULL)
   )
WHERE u.UserName IN ('admin', 'student')
ORDER BY u.UserName;
GO