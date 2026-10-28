using Abela.Application.Common;

namespace Abela.App.Common;

public static class TaskExtensions
{
    /// <summary>
    /// Awaits a fire-and-forget task (from a constructor or event handler) and surfaces any
    /// exception through the dialog service instead of silently swallowing it, which otherwise
    /// leaves the screen looking blank/broken with no indication anything went wrong.
    /// </summary>
    public static async void FireAndForgetSafe(this Task task, IDialogService dialog, string context)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            dialog.ShowError($"حدث خطأ أثناء تحميل البيانات ({context}):\n{ex.Message}");
        }
    }
}
