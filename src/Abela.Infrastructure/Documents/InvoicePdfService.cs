using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abela.Infrastructure.Documents;

public class InvoicePdfService
{
    private static readonly TextStyle BaseStyle = TextStyle.Default.FontFamily("Arial").FontSize(11);

    public byte[] GenerateInvoice(InvoicePdfData data, CompanyHeaderInfo company)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(BaseStyle);
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(company.CompanyName).FontSize(20).Bold();
                            if (!string.IsNullOrWhiteSpace(company.Phone)) c.Item().Text($"هاتف: {company.Phone}").FontSize(9);
                            if (!string.IsNullOrWhiteSpace(company.Address)) c.Item().Text(company.Address).FontSize(9);
                            if (!string.IsNullOrWhiteSpace(company.TaxNumber)) c.Item().Text($"الرقم الضريبي: {company.TaxNumber}").FontSize(9);
                        });
                        row.ConstantItem(180).Column(c =>
                        {
                            c.Item().AlignLeft().Text(data.Title).FontSize(16).Bold();
                            c.Item().AlignLeft().Text($"رقم: {data.InvoiceNumber}").FontSize(10);
                            c.Item().AlignLeft().Text($"التاريخ: {data.InvoiceDate:yyyy-MM-dd HH:mm}").FontSize(10);
                        });
                    });
                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"{data.PartyLabel}: {data.PartyName}").Bold();
                        if (!string.IsNullOrWhiteSpace(data.PartyPhone))
                            row.RelativeItem().AlignLeft().Text($"الهاتف: {data.PartyPhone}");
                    });
                    col.Item().PaddingTop(5).Text($"طريقة الدفع: {data.PaymentMethod}").FontSize(10);

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("الصنف");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("الكمية");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("السعر");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("الخصم");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("الإجمالي");
                        });

                        foreach (var line in data.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(line.ProductName);
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.Quantity.ToString("0.##"));
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.UnitPrice.ToString("0.00"));
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.Discount.ToString("0.00"));
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.LineTotal.ToString("0.00"));
                        }
                    });

                    col.Item().PaddingTop(15).AlignLeft().Width(260).Column(totals =>
                    {
                        AddTotalRow(totals, "الإجمالي قبل الخصم", data.SubTotal);
                        if (data.DiscountAmount != 0) AddTotalRow(totals, "الخصم", data.DiscountAmount);
                        if (data.TaxAmount != 0) AddTotalRow(totals, "الضريبة", data.TaxAmount);
                        if (data.ExtraCosts != 0) AddTotalRow(totals, "تكاليف إضافية", data.ExtraCosts);
                        totals.Item().PaddingTop(3).LineHorizontal(0.5f);
                        AddTotalRow(totals, "الإجمالي النهائي", data.TotalAmount, true);
                        AddTotalRow(totals, "المبلغ المدفوع", data.PaidAmount);
                        AddTotalRow(totals, "المبلغ المتبقي", data.RemainingAmount, true);
                    });

                    if (!string.IsNullOrWhiteSpace(data.Notes))
                        col.Item().PaddingTop(10).Text($"ملاحظات: {data.Notes}").FontSize(9);
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"أنشئت بواسطة: {data.CreatedBy}").FontSize(8);
                        row.RelativeItem().AlignLeft().Text(company.FooterText ?? "").FontSize(8);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateReturnDocument(ReturnPdfData data, CompanyHeaderInfo company)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(BaseStyle);
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(company.CompanyName).FontSize(18).Bold();
                        row.ConstantItem(200).AlignLeft().Column(c =>
                        {
                            c.Item().Text(data.Title).FontSize(15).Bold();
                            c.Item().Text($"رقم: {data.ReturnNumber}").FontSize(10);
                            c.Item().Text($"التاريخ: {data.ReturnDate:yyyy-MM-dd}").FontSize(10);
                            c.Item().Text($"الفاتورة الأصلية: {data.OriginalInvoiceNumber}").FontSize(10);
                        });
                    });
                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text($"{data.PartyLabel}: {data.PartyName}").Bold();
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); });
                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("الصنف");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("الكمية");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("السعر");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("الإجمالي");
                        });
                        foreach (var line in data.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(line.ProductName);
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.Quantity.ToString("0.##"));
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.UnitPrice.ToString("0.00"));
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.LineTotal.ToString("0.00"));
                        }
                    });
                    col.Item().PaddingTop(10).AlignLeft().Text($"الإجمالي: {data.TotalAmount:0.00}").Bold();
                    if (!string.IsNullOrWhiteSpace(data.Notes)) col.Item().PaddingTop(10).Text($"ملاحظات: {data.Notes}").FontSize(9);
                });

                page.Footer().Text($"أنشئ بواسطة: {data.CreatedBy}").FontSize(8);
            });
        });
        return document.GeneratePdf();
    }

    public byte[] GenerateVoucher(VoucherPdfData data, CompanyHeaderInfo company)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(30);
                page.DefaultTextStyle(BaseStyle);
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Text(company.CompanyName).FontSize(16).Bold();
                    col.Item().Text(data.Title).FontSize(14).Bold();
                    col.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text($"رقم السند: {data.Number}");
                    col.Item().Text($"التاريخ: {data.Date:yyyy-MM-dd HH:mm}");
                    col.Item().Text($"{data.PartyLabel}: {data.PartyName}");
                    col.Item().PaddingTop(10).Text($"المبلغ: {data.Amount:0.00}").FontSize(14).Bold();
                    col.Item().Text($"طريقة الدفع: {data.PaymentMethod}");
                    if (!string.IsNullOrWhiteSpace(data.ReferenceNumber)) col.Item().Text($"رقم مرجعي: {data.ReferenceNumber}");
                    if (!string.IsNullOrWhiteSpace(data.Notes)) col.Item().PaddingTop(5).Text($"ملاحظات: {data.Notes}").FontSize(9);
                });

                page.Footer().Text($"أنشئ بواسطة: {data.CreatedBy}").FontSize(8);
            });
        });
        return document.GeneratePdf();
    }

    public byte[] GenerateTabularReport(string title, string[] headers, IEnumerable<string[]> rows, CompanyHeaderInfo company, string? subtitle = null)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(BaseStyle.FontSize(9));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(company.CompanyName).FontSize(14).Bold();
                        row.RelativeItem().AlignLeft().Text(title).FontSize(14).Bold();
                    });
                    if (!string.IsNullOrWhiteSpace(subtitle)) col.Item().Text(subtitle).FontSize(9);
                    col.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c => { foreach (var _ in headers) c.RelativeColumn(); });
                    table.Header(h =>
                    {
                        foreach (var header in headers)
                            h.Cell().Element(HeaderCell).Text(header);
                    });
                    foreach (var row in rows)
                        foreach (var cell in row)
                            table.Cell().Element(BodyCell).Text(cell);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة ");
                    x.CurrentPageNumber();
                    x.Span(" من ");
                    x.TotalPages();
                });
            });
        });
        return document.GeneratePdf();
    }

    private static void AddTotalRow(ColumnDescriptor col, string label, decimal value, bool bold = false)
    {
        col.Item().Row(row =>
        {
            var text1 = row.RelativeItem().Text(label);
            var text2 = row.RelativeItem().AlignLeft().Text(value.ToString("0.00"));
            if (bold) { text1.Bold(); text2.Bold(); }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Darken3).Padding(5).DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
