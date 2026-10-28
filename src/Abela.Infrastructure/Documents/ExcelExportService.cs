using ClosedXML.Excel;

namespace Abela.Infrastructure.Documents;

public class ExcelExportService
{
    public void Export(string filePath, string sheetTitle, string[] headers, IEnumerable<object[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetTitle.Length > 31 ? sheetTitle[..31] : sheetTitle);
        sheet.RightToLeft = true;

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int r = 2;
        foreach (var row in rows)
        {
            for (int c = 0; c < row.Length; c++)
            {
                var cell = sheet.Cell(r, c + 1);
                var value = row[c];
                if (value is decimal or double or int) cell.Value = Convert.ToDouble(value);
                else if (value is DateTime dt) cell.Value = dt;
                else cell.Value = value?.ToString() ?? string.Empty;
            }
            r++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
