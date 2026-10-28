using Abela.Application.Common;
using Abela.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class BackupService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public BackupService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public string GetDatabaseFilePath()
    {
        var csb = new SqliteConnectionStringBuilder(_db.Database.GetConnectionString());
        return csb.DataSource;
    }

    public string SuggestBackupFileName() => $"Abela_Backup_{DateTime.Now:yyyy-MM-dd_HHmm}.db";

    public async Task<ServiceResult> CreateBackupAsync(string destinationFilePath)
    {
        try
        {
            await using var conn = new SqliteConnection(_db.Database.GetConnectionString());
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "VACUUM INTO $path";
            cmd.Parameters.AddWithValue("$path", destinationFilePath);
            await cmd.ExecuteNonQueryAsync();

            await _audit.LogAsync("Backup", $"إنشاء نسخة احتياطية: {Path.GetFileName(destinationFilePath)}");
            return ServiceResult.Ok("تم إنشاء النسخة الاحتياطية بنجاح");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"فشل إنشاء النسخة الاحتياطية: {ex.Message}");
        }
    }

    public bool ValidateBackupFile(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        try
        {
            using var conn = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users'";
            var result = cmd.ExecuteScalar();
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Restores from a backup file. Caller must dispose all DbContext instances and reopen the app afterward.</summary>
    public async Task<ServiceResult> RestoreBackupAsync(string sourceFilePath)
    {
        if (!ValidateBackupFile(sourceFilePath))
            return ServiceResult.Fail("ملف النسخة الاحتياطية غير صالح أو تالف");

        var dbPath = GetDatabaseFilePath();
        var safetyBackup = Path.Combine(Path.GetDirectoryName(dbPath)!, $"Abela_PreRestore_{DateTime.Now:yyyy-MM-dd_HHmmss}.db");

        try
        {
            SqliteConnection.ClearAllPools();
            File.Copy(dbPath, safetyBackup, overwrite: true);
            File.Copy(sourceFilePath, dbPath, overwrite: true);
            return ServiceResult.Ok("تمت استعادة النسخة الاحتياطية بنجاح، يجب إعادة تشغيل البرنامج الآن");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"فشلت عملية الاستعادة: {ex.Message}");
        }
    }
}
