using System.Globalization;
using System.Windows;
using Abela.Domain.Entities;
using Abela.Domain.Enums;

namespace Abela.App.Views;

public partial class ExpenseEditWindow : Window
{
    private readonly Expense _expense;

    public ExpenseEditWindow(Expense expense, List<ExpenseCategory> categories, bool isNew)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _expense = expense;

        CategoryCombo.ItemsSource = categories;
        MethodCombo.ItemsSource = Enum.GetValues<PaymentMethod>();

        DatePickerBox.SelectedDate = expense.ExpenseDate == default ? DateTime.Today : expense.ExpenseDate;
        CategoryCombo.SelectedItem = categories.FirstOrDefault(c => c.Id == expense.ExpenseCategoryId);
        DescriptionBox.Text = expense.Description;
        AmountBox.Text = expense.Amount == 0 ? "" : expense.Amount.ToString(CultureInfo.InvariantCulture);
        MethodCombo.SelectedItem = expense.PaymentMethod;
        PayeeBox.Text = expense.PayeeName;
        ReferenceBox.Text = expense.ReferenceNumber;
        NotesBox.Text = expense.Notes;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        if (CategoryCombo.SelectedItem is not ExpenseCategory category) { ShowError("الرجاء اختيار تصنيف المصروف"); return; }
        if (string.IsNullOrWhiteSpace(DescriptionBox.Text)) { ShowError("وصف المصروف مطلوب"); return; }
        if (!decimal.TryParse(AmountBox.Text, out var amount) || amount <= 0) { ShowError("الرجاء إدخال مبلغ صحيح أكبر من صفر"); return; }
        if (MethodCombo.SelectedItem is not PaymentMethod method) return;

        _expense.ExpenseDate = DatePickerBox.SelectedDate ?? DateTime.Today;
        _expense.ExpenseCategoryId = category.Id;
        _expense.Description = DescriptionBox.Text.Trim();
        _expense.Amount = amount;
        _expense.PaymentMethod = method;
        _expense.PayeeName = PayeeBox.Text;
        _expense.ReferenceNumber = ReferenceBox.Text;
        _expense.Notes = NotesBox.Text;

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
