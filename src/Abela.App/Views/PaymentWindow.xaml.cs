using System.Windows;
using Abela.Domain.Enums;

namespace Abela.App.Views;

public class PartyOption
{
    public object Party { get; set; } = null!;
    public string Name { get; set; } = "";
}

public partial class PaymentWindow : Window
{
    public object? SelectedParty { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod SelectedMethod { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }

    public PaymentWindow(string title, List<object> parties, Func<object, string> nameSelector)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        TitleText.Text = title;
        PartyCombo.ItemsSource = parties.Select(p => new PartyOption { Party = p, Name = nameSelector(p) }).ToList();
        MethodCombo.ItemsSource = Enum.GetValues<PaymentMethod>();
        MethodCombo.SelectedItem = PaymentMethod.Cash;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        if (PartyCombo.SelectedItem is not PartyOption option) { ShowError("الرجاء اختيار الجهة"); return; }
        if (!decimal.TryParse(AmountBox.Text, out var amount) || amount <= 0) { ShowError("الرجاء إدخال مبلغ صحيح أكبر من صفر"); return; }
        if (MethodCombo.SelectedItem is not PaymentMethod method) return;

        SelectedParty = option.Party;
        Amount = amount;
        SelectedMethod = method;
        Reference = ReferenceBox.Text;
        Notes = NotesBox.Text;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
