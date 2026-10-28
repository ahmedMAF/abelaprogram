using System.Windows;
using Abela.Application.Common;
using Microsoft.Win32;

namespace Abela.App.Common;

public class DialogService : IDialogService
{
    public void ShowInfo(string message, string title = "تنبيه") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message, string title = "خطأ") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowSuccess(string message, string title = "نجاح") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string message, string title = "تأكيد") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public string? SaveFile(string filter, string defaultFileName, string title = "حفظ باسم")
    {
        var dlg = new SaveFileDialog { Filter = filter, FileName = defaultFileName, Title = title };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? OpenFile(string filter, string title = "اختر ملفًا")
    {
        var dlg = new OpenFileDialog { Filter = filter, Title = title };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SelectFolder(string title = "اختر مجلدًا")
    {
        var dlg = new OpenFolderDialog { Title = title };
        return dlg.ShowDialog() == true ? dlg.FolderName : null;
    }
}
