using Abela.Application.Common;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly AuditService _audit;

    public AuthService(AppDbContext db, ICurrentUserService currentUser, AuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<ServiceResult<bool>> LoginAsync(string username, string password)
    {
        var user = await _db.Users
            .Include(u => u.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return ServiceResult<bool>.Fail("اسم المستخدم أو كلمة المرور غير صحيحة");

        if (!user.IsActive)
            return ServiceResult<bool>.Fail("هذا الحساب معطل، الرجاء التواصل مع المدير");

        var permissions = user.Role.RolePermissions.Select(rp => rp.Permission.Code);
        _currentUser.SetUser(user.Id, user.FullName, user.Role.Name, permissions);

        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Login", $"تسجيل دخول المستخدم {user.Username}");

        return ServiceResult<bool>.Ok(user.MustChangePassword, "تم تسجيل الدخول بنجاح");
    }

    public async Task LogoutAsync()
    {
        await _audit.LogAsync("Logout", $"تسجيل خروج المستخدم {_currentUser.UserName}");
        _currentUser.Clear();
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return ServiceResult.Fail("المستخدم غير موجود");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return ServiceResult.Fail("كلمة المرور الحالية غير صحيحة");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return ServiceResult.Fail("يجب أن تتكون كلمة المرور الجديدة من 6 أحرف على الأقل");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "تغيير كلمة المرور", "User", user.Id);
        return ServiceResult.Ok("تم تغيير كلمة المرور بنجاح");
    }
}
