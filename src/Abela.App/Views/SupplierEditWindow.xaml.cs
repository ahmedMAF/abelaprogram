using System.Globalization;
using System.Windows;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class SupplierEditWindow : Window
{
    private readonly Supplier _supplier;

    public SupplierEditWindow(Supplier supplier)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _supplier = supplier;

        CodeBox.Text = supplier.Code;
        NameBox.Text = supplier.Name;
        CompanyBox.Text = supplier.CompanyName;
        PhoneBox.Text = supplier.Phone;
        WhatsAppBox.Text = supplier.WhatsApp;
        EmailBox.Text = supplier.Email;
        AddressBox.Text = supplier.Address;
        TaxNumberBox.Text = supplier.TaxNumber;
        OpeningBalanceBox.Text = supplier.OpeningBalance.ToString(CultureInfo.InvariantCulture);
        NotesBox.Text = supplier.Notes;
        IsActiveCheck.IsChecked = supplier.IsActive;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { ShowError("اسم المورد مطلوب"); return; }
        if (!decimal.TryParse(OpeningBalanceBox.Text, out var opening)) opening = 0;

        _supplier.Code = string.IsNullOrWhiteSpace(CodeBox.Text) ? _supplier.Code : CodeBox.Text.Trim();
        _supplier.Name = NameBox.Text.Trim();
        _supplier.CompanyName = string.IsNullOrWhiteSpace(CompanyBox.Text) ? null : CompanyBox.Text.Trim();
        _supplier.Phone = PhoneBox.Text;
        _supplier.WhatsApp = WhatsAppBox.Text;
        _supplier.Email = EmailBox.Text;
        _supplier.Address = AddressBox.Text;
        _supplier.TaxNumber = TaxNumberBox.Text;
        _supplier.OpeningBalance = opening;
        _supplier.Notes = NotesBox.Text;
        _supplier.IsActive = IsActiveCheck.IsChecked == true;

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
