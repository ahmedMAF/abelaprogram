using System.Windows;

namespace Abela.App.Views;

public partial class InputDialog : Window
{
    private InputDialog(string title, string prompt)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        Title = title;
        PromptText.Text = prompt;
    }

    public static string? Show(string title, string prompt)
    {
        var dialog = new InputDialog(title, prompt);
        return dialog.ShowDialog() == true ? dialog.InputBox.Text.Trim() : null;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
