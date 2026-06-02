using System.Text;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PharmacyManagement.Infrastructure.Export;

public interface IExportService
{
    Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName) where T : class;
    Task<byte[]> ExportToCsvAsync<T>(List<T> data) where T : class;
    Task<byte[]> ExportToPdfAsync<T>(List<T> data, string title) where T : class;
}

public class ExportService : IExportService
{
    public Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName) where T : class
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        if (data == null || data.Count == 0)
        {
            worksheet.Cells["A1"].Value = "No data available";
            return Task.FromResult(package.GetAsByteArray());
        }

        var properties = typeof(T)
            .GetProperties()
            .Where(p => p.CanRead)
            .ToArray();

        // Headers
        for (int col = 0; col < properties.Length; col++)
        {
            worksheet.Cells[1, col + 1].Value = properties[col].Name;
            worksheet.Cells[1, col + 1].Style.Font.Bold = true;
        }

        // Data
        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(data[row]);

                worksheet.Cells[row + 2, col + 1].Value =
                    value?.ToString() ?? string.Empty;
            }
        }

        worksheet.Cells.AutoFitColumns();

        return Task.FromResult(package.GetAsByteArray());
    }
    public Task<byte[]> ExportToCsvAsync<T>(List<T> data) where T : class
    {
        var sb = new StringBuilder();

        if (data == null || data.Count == 0)
        {
            sb.AppendLine("No data available");
            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead)
            .ToArray();

        // Headers
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(p.Name))));

        // Rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item)?.ToString() ?? string.Empty;
                return EscapeCsv(value);
            });

            sb.AppendLine(string.Join(",", values));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }


public Task<byte[]> ExportToPdfAsync<T>(List<T> data, string title) where T : class
{
    QuestPDF.Settings.License = LicenseType.Community;

    var document = QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);

            // ================= HEADER =================
            page.Header().Column(header =>
            {
                header.Item().Text(title)
                    .FontSize(18)
                    .Bold()
                    .AlignCenter();

                header.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
                    .FontSize(10)
                    .AlignCenter();

                header.Item().LineHorizontal(1);
            });

            // ================= CONTENT =================
            page.Content().Column(content =>
            {
                content.Spacing(5);

                if (data == null || !data.Any())
                {
                    content.Item().Text("No data available");
                    return;
                }

                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead &&
                        (p.PropertyType.IsPrimitive ||
                         p.PropertyType == typeof(string) ||
                         p.PropertyType.IsValueType ||
                         Nullable.GetUnderlyingType(p.PropertyType)?.IsValueType == true))
                    .ToArray();

                // TABLE
                content.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (int i = 0; i < properties.Length; i++)
                            columns.RelativeColumn();
                    });

                    // HEADER
                    table.Header(header =>
                    {
                        foreach (var prop in properties)
                        {
                            header.Cell()
                                .BorderBottom(1)
                                .Padding(5)
                                .Text(prop.Name)
                                .Bold();
                        }
                    });

                    // ROWS
                    foreach (var item in data.Take(100))
                    {
                        foreach (var prop in properties)
                        {
                            var value = prop.GetValue(item)?.ToString() ?? "";

                            table.Cell()
                                .BorderBottom(0.5f)
                                .Padding(5)
                                .Text(value);
                        }
                    }
                });

                if (data.Count > 100)
                {
                    content.Item()
                        .PaddingTop(10)
                        .Text($"... and {data.Count - 100} more records");
                }
            });

            // ================= FOOTER =================
            page.Footer()
                .AlignCenter()
                .Text($"Total Records: {data?.Count ?? 0}");
        });
    });

    byte[] pdfBytes = document.GeneratePdf();
    return Task.FromResult(pdfBytes);
}
private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
