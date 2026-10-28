using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public UserService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public Task<List<User>> GetAllAsync() => _db.Users.Include(u => u.Role).OrderBy(u => u.Username).ToListAsync();
    public Task<User?> GetByIdAsync(int id) => _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
    public Task<List<Role>> GetRolesAsync() => _db.Roles.OrderBy(r => r.Name).ToListAsync();
    public Task<List<Permission>> GetPermissionsAsync() => _db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Name).ToListAsync();
    public Task<List<int>> GetRolePermissionIdsAsync(int roleId) => _db.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.PermissionId).ToListAsync();

    public async Task<ServiceResult> SaveRolePermissionsAsync(int roleId, List<int> permissionIds)
    {
        var existing = _db.RolePermissions.Where(rp => rp.RoleId == roleId);
        _db.RolePermissions.RemoveRange(existing);
        foreach (var pid in permissionIds)
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "تعديل صلاحيات الدور", "Role", roleId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SaveRoleAsync(Role role)
    {
        if (string.IsNullOrWhiteSpace(role.Name)) return ServiceResult.Fail("اسم الدور مطلوب");
        if (role.Id == 0) _db.Roles.Add(role); else _db.Roles.Update(role);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CreateUserAsync(string username, string password, string fullName, int roleId)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3) return ServiceResult.Fail("اسم المستخدم يجب أن يكون 3 أحرف على الأقل");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return ServiceResult.Fail("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username)) return ServiceResult.Fail("اسم المستخدم مستخدم بالفعل");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            RoleId = roleId,
            MustChangePassword = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", $"إنشاء مستخدم {username}", "User", user.Id);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateUserAsync(int id, string fullName, int roleId, bool isActive)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return ServiceResult.Fail("المستخدم غير موجود");
        user.FullName = fullName;
        user.RoleId = roleId;
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", $"تعديل مستخدم {user.Username}", "User", id);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ResetPasswordAsync(int id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6) return ServiceResult.Fail("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
        var user = await _db.Users.FindAsync(id);
        if (user is null) return ServiceResult.Fail("المستخدم غير موجود");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = true;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", $"إعادة تعيين كلمة مرور {user.Username}", "User", id);
        return ServiceResult.Ok();
    }
}

public class AuditLogService
{
    private readonly AppDbContext _db;
    public AuditLogService(AppDbContext db) { _db = db; }

    public async Task<List<AuditLog>> SearchAsync(DateTime? from = null, DateTime? to = null, int? userId = null)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from);
        if (to.HasValue) q = q.Where(a => a.CreatedAt <= to);
        if (userId.HasValue) q = q.Where(a => a.UserId == userId);
        return await q.OrderByDescending(a => a.CreatedAt).Take(2000).ToListAsync();
    }
}
