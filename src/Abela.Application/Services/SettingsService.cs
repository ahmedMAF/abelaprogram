using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class SettingsService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public SettingsService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public Task<ApplicationSettings> GetApplicationSettingsAsync() => _db.ApplicationSettings.FirstAsync();
    public Task<CompanySettings> GetCompanySettingsAsync() => _db.CompanySettings.FirstAsync();

    public async Task<ServiceResult> SaveApplicationSettingsAsync(ApplicationSettings settings)
    {
        _db.ApplicationSettings.Update(settings);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("SettingsChange", "تعديل إعدادات النظام");
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SaveCompanySettingsAsync(CompanySettings settings)
    {
        _db.CompanySettings.Update(settings);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("SettingsChange", "تعديل بيانات الشركة");
        return ServiceResult.Ok();
    }

    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync() => await _db.ExpenseCategories.OrderBy(c => c.Name).ToListAsync();

    public async Task<ServiceResult> SaveExpenseCategoryAsync(ExpenseCategory category)
    {
        if (string.IsNullOrWhiteSpace(category.Name)) return ServiceResult.Fail("اسم التصنيف مطلوب");
        if (category.Id == 0) _db.ExpenseCategories.Add(category);
        else _db.ExpenseCategories.Update(category);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteExpenseCategoryAsync(int id)
    {
        var used = await _db.Expenses.AnyAsync(e => e.ExpenseCategoryId == id);
        if (used) return ServiceResult.Fail("لا يمكن حذف التصنيف لارتباطه بمصروفات");
        var category = await _db.ExpenseCategories.FindAsync(id);
        if (category is null) return ServiceResult.Fail("التصنيف غير موجود");
        _db.ExpenseCategories.Remove(category);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
