using Abela.Domain.Common;
using Abela.Domain.Enums;

namespace Abela.Domain.Entities;

public class ProductCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Unit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public ProductCategory Category { get; set; } = null!;
    public string? Description { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal MinStockLevel { get; set; }
    public string? Brand { get; set; }
    public string? ModelNumber { get; set; }
    public bool RequiresSerialNumber { get; set; }
    public int? WarrantyMonths { get; set; }
    public string? Notes { get; set; }

    public ICollection<ProductSerialNumber> SerialNumbers { get; set; } = new List<ProductSerialNumber>();
}

public class ProductSerialNumber : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string SerialNumber { get; set; } = string.Empty;
    public SerialStatus Status { get; set; } = SerialStatus.InStock;
    public int? PurchaseInvoiceItemId { get; set; }
    public PurchaseInvoiceItem? PurchaseInvoiceItem { get; set; }
    public int? SalesInvoiceItemId { get; set; }
    public SalesInvoiceItem? SalesInvoiceItem { get; set; }
}
