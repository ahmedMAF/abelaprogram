using System.Collections.ObjectModel;
using System.IO;
using Abela.Application.Common;
using Abela.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class BackupViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly IDialogService _dialog;

    public ObservableCollection<string> BackupFiles { get; } = new();

    public BackupViewModel(BackupService backupService, IDialogService dialog)
    {
        _backupService = backupService;
        _dialog = dialog;
        RefreshList();
    }

    private string BackupsFolder => Path.Combine(App.DataFolder, "Backups");

    private void RefreshList()
    {
        BackupFiles.Clear();
        if (!Directory.Exists(BackupsFolder)) return;
        foreach (var file in Directory.GetFiles(BackupsFolder, "*.db").OrderByDescending(f => f))
            BackupFiles.Add(Path.GetFileName(file));
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        var suggestedName = _backupService.SuggestBackupFileName();
        var path = _dialog.SaveFile("Database Files|*.db", suggestedName, "حفظ النسخة الاحتياطية");
        if (path is null) return;

        var result = await _backupService.CreateBackupAsync(path);
        if (result.Success)
        {
            _dialog.ShowSuccess(result.Message);
            if (Path.GetDirectoryName(path) == BackupsFolder) RefreshList();
        }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task CreateQuickBackupAsync()
    {
        Directory.CreateDirectory(BackupsFolder);
        var path = Path.Combine(BackupsFolder, _backupService.SuggestBackupFileName());
        var result = await _backupService.CreateBackupAsync(path);
        if (result.Success) { _dialog.ShowSuccess(result.Message); RefreshList(); }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        var path = _dialog.OpenFile("Database Files|*.db", "اختر ملف النسخة الاحتياطية");
        if (path is null) return;

        if (!_dialog.Confirm("تحذير: ستحل هذه العملية محل جميع البيانات الحالية بالبيانات الموجودة في النسخة الاحتياطية. سيتم إنشاء نسخة احتياطية أمان من البيانات الحالية أولاً. هل تريد المتابعة؟"))
            return;

        var result = await _backupService.RestoreBackupAsync(path);
        if (result.Success)
        {
            _dialog.ShowSuccess(result.Message);
            System.Windows.Application.Current.Shutdown();
        }
        else _dialog.ShowError(result.Message);
    }
}
