using System.Globalization;
using System.Text;

namespace Salzee.Services;

public static class AmsCsvExportHelper
{
    public static byte[] ToUtf8Csv(string[] headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public static string F(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    public static string F(DateTime? value, string format = "yyyy-MM-dd") =>
        value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : "";

    public static string S(object? value) => value?.ToString() ?? "";

    public static string Escape(string? value)
    {
        var s = value ?? "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
