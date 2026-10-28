using System.Globalization;
using System.Windows;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class CustomerEditWindow : Window
{
    private readonly Customer _customer;

    public CustomerEditWindow(Customer customer)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _customer = customer;

        CodeBox.Text = customer.Code;
        NameBox.Text = customer.Name;
        CompanyBox.Text = customer.CompanyName;
        PhoneBox.Text = customer.Phone;
        WhatsAppBox.Text = customer.WhatsApp;
        EmailBox.Text = customer.Email;
        AddressBox.Text = customer.Address;
        TaxNumberBox.Text = customer.TaxNumber;
        OpeningBalanceBox.Text = customer.OpeningBalance.ToString(CultureInfo.InvariantCulture);
        CreditLimitBox.Text = customer.CreditLimit?.ToString(CultureInfo.InvariantCulture);
        NotesBox.Text = customer.Notes;
        IsActiveCheck.IsChecked = customer.IsActive;

        if (customer.IsDefault)
        {
            NameBox.IsEnabled = false;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { ShowError("اسم العميل مطلوب"); return; }
        if (!decimal.TryParse(OpeningBalanceBox.Text, out var opening)) opening = 0;
        decimal? creditLimit = decimal.TryParse(CreditLimitBox.Text, out var cl) ? cl : null;

        _customer.Code = string.IsNullOrWhiteSpace(CodeBox.Text) ? _customer.Code : CodeBox.Text.Trim();
        _customer.Name = NameBox.Text.Trim();
        _customer.CompanyName = string.IsNullOrWhiteSpace(CompanyBox.Text) ? null : CompanyBox.Text.Trim();
        _customer.Phone = PhoneBox.Text;
        _customer.WhatsApp = WhatsAppBox.Text;
        _customer.Email = EmailBox.Text;
        _customer.Address = AddressBox.Text;
        _customer.TaxNumber = TaxNumberBox.Text;
        _customer.OpeningBalance = opening;
        _customer.CreditLimit = creditLimit;
        _customer.Notes = NotesBox.Text;
        _customer.IsActive = IsActiveCheck.IsChecked == true;

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
