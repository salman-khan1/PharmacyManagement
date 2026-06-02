using System.Text;

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
        try
        {
            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            if (data == null || data.Count == 0)
            {
                worksheet.Cells[1, 1].Value = "No data available";
                return Task.FromResult(package.GetAsByteArray());
            }

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && !p.PropertyType.IsClass)
                .ToArray();

            // Headers
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = properties[i].Name;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Data
            for (int row = 0; row < data.Count; row++)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    var value = properties[col].GetValue(data[row]);
                    worksheet.Cells[row + 2, col + 1].Value = value;
                }
            }

            worksheet.Cells.AutoFitColumns();
            return Task.FromResult(package.GetAsByteArray());
        }
        catch
        {
            using var fallbackPackage = new OfficeOpenXml.ExcelPackage();
            fallbackPackage.Workbook.Worksheets.Add("Error");
            return Task.FromResult(fallbackPackage.GetAsByteArray());
        }
    }

    public Task<byte[]> ExportToCsvAsync<T>(List<T> data) where T : class
    {
        try
        {
            var sb = new StringBuilder();

            if (data == null || data.Count == 0)
            {
                sb.AppendLine("No data available");
                return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
            }

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && !p.PropertyType.IsClass)
                .ToArray();

            // Headers
            sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(p.Name))));

            // Data rows
            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item);
                    return EscapeCsv(value?.ToString() ?? "");
                });
                sb.AppendLine(string.Join(",", values));
            }

            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }
        catch
        {
            return Task.FromResult(Encoding.UTF8.GetBytes("Error exporting data"));
        }
    }

    public Task<byte[]> ExportToPdfAsync<T>(List<T> data, string title) where T : class
    {
        try
        {
            // Create a simple text-based report formatted as PDF-like content
            // This is a simplified approach - for production, use a proper PDF library
            var sb = new StringBuilder();

            sb.AppendLine($"{'='}{new string('=', 78)}");
            sb.AppendLine($"  {title}".PadRight(79));
            sb.AppendLine($"  Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}".PadRight(79));
            sb.AppendLine($"{'='}{new string('=', 78)}");
            sb.AppendLine();

            if (data == null || data.Count == 0)
            {
                sb.AppendLine("  No data available.");
                sb.AppendLine();
                sb.AppendLine($"{'='}{new string('=', 78)}");
                return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
            }

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && !p.PropertyType.IsClass)
                .ToArray();

            // Build header
            foreach (var prop in properties)
            {
                sb.Append($"  {prop.Name,-18}");
            }
            sb.AppendLine();
            sb.AppendLine($"  {new string('-', properties.Length * 20)}");

            // Build rows (limit to first 100)
            foreach (var item in data.Take(100))
            {
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item)?.ToString() ?? "";
                    if (value.Length > 16) value = value[..16];
                    sb.Append($"  {value,-18}");
                }
                sb.AppendLine();
            }

            if (data.Count > 100)
            {
                sb.AppendLine();
                sb.AppendLine($"  ... and {data.Count - 100} more rows");
            }

            sb.AppendLine();
            sb.AppendLine($"  Total Records: {data.Count}");
            sb.AppendLine($"{'='}{new string('=', 78)}");

            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }
        catch (Exception ex)
        {
            var errorText = $"PDF Export Error: {ex.Message}";
            return Task.FromResult(Encoding.UTF8.GetBytes(errorText));
        }
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
