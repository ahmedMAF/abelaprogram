using System.Windows;
using System.Windows.Controls;

namespace Abela.App.Views;

public partial class SerialEntryWindow : Window
{
    private readonly bool _isSelectionMode;
    private readonly int _requiredCount;
    private readonly List<TextBox> _textBoxes = new();
    private readonly List<CheckBox> _checkBoxes = new();

    public List<string> SelectedSerials { get; private set; } = new();

    /// <summary>Purchase mode: free-text entry of new serial numbers.</summary>
    public SerialEntryWindow(string productName, int quantity)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _isSelectionMode = false;
        _requiredCount = quantity;
        InfoText.Text = $"أدخل {quantity} رقم تسلسلي للمنتج: {productName}";

        var panel = new StackPanel();
        for (int i = 0; i < quantity; i++)
        {
            var tb = new TextBox { Margin = new Thickness(0, 0, 0, 6), Tag = i };
            _textBoxes.Add(tb);
            panel.Children.Add(tb);
        }
        SerialsPanel.Content = panel;
    }

    /// <summary>Sale mode: select from available in-stock serials.</summary>
    public SerialEntryWindow(string productName, int quantity, List<string> availableSerials)
    {
        InitializeComponent();
        _isSelectionMode = true;
        _requiredCount = quantity;
        InfoText.Text = $"اختر {quantity} من الأرقام التسلسلية المتوفرة للمنتج: {productName}";

        var panel = new StackPanel();
        foreach (var serial in availableSerials)
        {
            var cb = new CheckBox { Content = serial, Tag = serial, Margin = new Thickness(0, 0, 0, 6) };
            _checkBoxes.Add(cb);
            panel.Children.Add(cb);
        }
        SerialsPanel.Content = panel;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        if (_isSelectionMode)
        {
            var selected = _checkBoxes.Where(c => c.IsChecked == true).Select(c => (string)c.Tag).ToList();
            if (selected.Count != _requiredCount)
            {
                ShowError($"الرجاء اختيار {_requiredCount} بالضبط");
                return;
            }
            SelectedSerials = selected;
        }
        else
        {
            var values = _textBoxes.Select(t => t.Text.Trim()).ToList();
            if (values.Any(string.IsNullOrWhiteSpace))
            {
                ShowError("الرجاء إدخال جميع الأرقام التسلسلية");
                return;
            }
            if (values.Distinct().Count() != values.Count)
            {
                ShowError("لا يمكن تكرار نفس الرقم التسلسلي في نفس الفاتورة");
                return;
            }
            SelectedSerials = values;
        }

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
