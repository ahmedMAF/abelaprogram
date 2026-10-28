namespace Abela.Domain.Common;

public static class PermissionCodes
{
    public const string SalesView = "Sales.View";
    public const string SalesAdd = "Sales.Add";
    public const string SalesEdit = "Sales.Edit";
    public const string SalesCancel = "Sales.Cancel";
    public const string SalesPrint = "Sales.Print";
    public const string SalesEditPrice = "Sales.EditPrice";
    public const string SalesViewCost = "Sales.ViewCost";

    public const string PurchasesView = "Purchases.View";
    public const string PurchasesAdd = "Purchases.Add";
    public const string PurchasesEdit = "Purchases.Edit";
    public const string PurchasesCancel = "Purchases.Cancel";

    public const string ProductsView = "Products.View";
    public const string ProductsAdd = "Products.Add";
    public const string ProductsEdit = "Products.Edit";
    public const string ProductsEditPurchasePrice = "Products.EditPurchasePrice";

    public const string InventoryView = "Inventory.View";
    public const string InventoryAdjust = "Inventory.Adjust";

    public const string CustomersView = "Customers.View";
    public const string CustomersAdd = "Customers.Add";
    public const string CustomersEdit = "Customers.Edit";

    public const string SuppliersView = "Suppliers.View";
    public const string SuppliersAdd = "Suppliers.Add";
    public const string SuppliersEdit = "Suppliers.Edit";

    public const string PaymentsView = "Payments.View";
    public const string PaymentsAdd = "Payments.Add";

    public const string ExpensesView = "Expenses.View";
    public const string ExpensesAdd = "Expenses.Add";
    public const string ExpensesEdit = "Expenses.Edit";
    public const string ExpensesCancel = "Expenses.Cancel";

    public const string ReportsView = "Reports.View";
    public const string ReportsViewProfit = "Reports.ViewProfit";
    public const string ReportsExport = "Reports.Export";

    public const string UsersManage = "Users.Manage";
    public const string SettingsManage = "Settings.Manage";
    public const string BackupManage = "Backup.Manage";
    public const string AuditView = "Audit.View";

    public static readonly (string Code, string Name, string Module)[] All =
    {
        (SalesView, "عرض المبيعات", "المبيعات"),
        (SalesAdd, "إضافة فاتورة بيع", "المبيعات"),
        (SalesEdit, "تعديل فاتورة بيع", "المبيعات"),
        (SalesCancel, "إلغاء فاتورة بيع", "المبيعات"),
        (SalesPrint, "طباعة فاتورة بيع", "المبيعات"),
        (SalesEditPrice, "تعديل سعر البيع", "المبيعات"),
        (SalesViewCost, "عرض التكلفة والأرباح", "المبيعات"),

        (PurchasesView, "عرض المشتريات", "المشتريات"),
        (PurchasesAdd, "إضافة فاتورة شراء", "المشتريات"),
        (PurchasesEdit, "تعديل فاتورة شراء", "المشتريات"),
        (PurchasesCancel, "إلغاء فاتورة شراء", "المشتريات"),

        (ProductsView, "عرض المنتجات", "المنتجات"),
        (ProductsAdd, "إضافة منتج", "المنتجات"),
        (ProductsEdit, "تعديل منتج", "المنتجات"),
        (ProductsEditPurchasePrice, "تعديل سعر الشراء", "المنتجات"),

        (InventoryView, "عرض المخزون", "المخزون"),
        (InventoryAdjust, "تسوية المخزون", "المخزون"),

        (CustomersView, "عرض العملاء", "العملاء"),
        (CustomersAdd, "إضافة عميل", "العملاء"),
        (CustomersEdit, "تعديل عميل", "العملاء"),

        (SuppliersView, "عرض الموردين", "الموردون"),
        (SuppliersAdd, "إضافة مورد", "الموردون"),
        (SuppliersEdit, "تعديل مورد", "الموردون"),

        (PaymentsView, "عرض الدفعات", "الدفعات"),
        (PaymentsAdd, "إضافة دفعة", "الدفعات"),

        (ExpensesView, "عرض المصروفات", "المصروفات"),
        (ExpensesAdd, "إضافة مصروف", "المصروفات"),
        (ExpensesEdit, "تعديل مصروف", "المصروفات"),
        (ExpensesCancel, "إلغاء مصروف", "المصروفات"),

        (ReportsView, "عرض التقارير", "التقارير"),
        (ReportsViewProfit, "عرض تقارير الأرباح", "التقارير"),
        (ReportsExport, "تصدير التقارير", "التقارير"),

        (UsersManage, "إدارة المستخدمين", "النظام"),
        (SettingsManage, "إدارة الإعدادات", "النظام"),
        (BackupManage, "النسخ الاحتياطي", "النظام"),
        (AuditView, "عرض سجل النشاط", "النظام"),
    };
}
