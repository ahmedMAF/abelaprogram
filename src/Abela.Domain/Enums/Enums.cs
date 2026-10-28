namespace Abela.Domain.Enums;

public enum SaleType
{
    Cash = 1,
    Credit = 2,
    Partial = 3
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    EWallet = 4,
    Mixed = 5,
    Credit = 6
}

public enum DocumentStatus
{
    Active = 1,
    Cancelled = 2
}

public enum SerialStatus
{
    InStock = 1,
    Sold = 2,
    Returned = 3
}

public enum InventoryTransactionType
{
    OpeningBalance = 1,
    Purchase = 2,
    Sale = 3,
    SalesReturn = 4,
    PurchaseReturn = 5,
    AdjustmentIncrease = 6,
    AdjustmentDecrease = 7,
    InvoiceCancellation = 8
}

public enum RefundMethod
{
    CashRefund = 1,
    DeductFromBalance = 2
}

public enum AuditActionType
{
    Login = 1,
    Logout = 2,
    Create = 3,
    Update = 4,
    Delete = 5,
    Cancel = 6,
    Payment = 7,
    StockAdjustment = 8,
    Backup = 9,
    Restore = 10,
    SettingsChange = 11,
    PriceChange = 12
}
