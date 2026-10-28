namespace Abela.Application.Common;

public class SalesLineInput
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
}

public class PurchaseLineInput
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
}
