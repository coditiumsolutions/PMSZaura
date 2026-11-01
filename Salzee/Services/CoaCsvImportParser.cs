using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Salzee.Services;

/// <summary>Parses client COA CSV (COA Code, Narration, Level #) or AMS 7-column import format.</summary>
public static class CoaCsvImportParser
{
    public enum CoaCsvFormat { Unknown, Client, Ams }

    public sealed class CoaImportRow
    {
        public int SourceLineNo { get; init; }
        public string AccountCode { get; init; } = "";
        public string AccountName { get; init; } = "";
        public int AccountCategoryId { get; init; }
        public string ParentAccountCode { get; init; } = "";
        public byte AccountLevel { get; init; }
        public bool IsControlAccount { get; init; }
        public bool AllowDirectPosting { get; init; }
    }

    private static readonly string[] ZeroByIndex = { "", "00", "000", "0000", "00000" };

    private static readonly Dictionary<string, int> CategoryIdByLeadingDigit = new(StringComparer.Ordinal)
    {
        ["1"] = 1,
        ["2"] = 2,
        ["3"] = 3,
        ["4"] = 4,
        ["5"] = 5,
        ["9"] = 2, // Suspense → Liability until a dedicated category exists
    };

    public static CoaCsvFormat DetectFormat(string? headerLine)
    {
        if (string.IsNullOrWhiteSpace(headerLine))
            return CoaCsvFormat.Unknown;
        var headers = SplitCsvLine(headerLine);
        if (headers.Any(h => h.Equals("AccountCode", StringComparison.OrdinalIgnoreCase)))
            return CoaCsvFormat.Ams;
        if (headers.Any(h => h.Equals("COA Code", StringComparison.OrdinalIgnoreCase)))
            return CoaCsvFormat.Client;
        return CoaCsvFormat.Unknown;
    }

    public sealed class CoaParseResult
    {
        public IReadOnlyList<CoaImportRow> Rows { get; init; } = Array.Empty<CoaImportRow>();
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    }

    public static CoaParseResult ParseFile(
        TextReader reader,
        CoaCsvFormat format,
        string? headerLine,
        IReadOnlyDictionary<string, int> categoryIdByName)
    {
        var errors = new List<string>();
        var rows = new List<CoaImportRow>();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            errors.Add("File is empty or missing header row.");
            return new CoaParseResult { Errors = errors };
        }

        var headers = SplitCsvLine(headerLine);
        var col = BuildColumnMap(headers);
        var lineNo = 1;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = SplitCsvLine(line);
            try
            {
                var row = format == CoaCsvFormat.Ams
                    ? ParseAmsRow(parts, col, lineNo, categoryIdByName, errors)
                    : ParseClientRow(parts, col, lineNo, categoryIdByName, errors);
                if (row != null)
                    rows.Add(row);
            }
            catch (Exception ex)
            {
                errors.Add($"Line {lineNo}: {ex.Message}");
            }
        }

        var ordered = rows
            .OrderBy(r => r.AccountLevel)
            .ThenBy(r => r.AccountCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new CoaParseResult { Rows = ordered, Errors = errors };
    }

    private static CoaImportRow? ParseClientRow(
        IReadOnlyList<string> parts,
        IReadOnlyDictionary<string, int> col,
        int lineNo,
        IReadOnlyDictionary<string, int> categoryIdByName,
        List<string> errors)
    {
        var code = Get(parts, col, "COA Code", "COACode");
        var name = Get(parts, col, "Narration", "AccountName");
        var levelText = Get(parts, col, "Level #", "Level", "AccountLevel");
        var categoryText = Get(parts, col, "Account Category", "AccountCategory");

        if (string.IsNullOrWhiteSpace(code))
        {
            errors.Add($"Line {lineNo}: COA Code is required.");
            return null;
        }

        if (!byte.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
            || level is < 1 or > 5)
        {
            errors.Add($"Line {lineNo}: Level # must be 1–5.");
            return null;
        }

        if (!TryResolveCategoryId(code, categoryText, categoryIdByName, lineNo, errors, out var catId))
            return null;

        var parentCode = DeriveParentAccountCode(code, level);
        return new CoaImportRow
        {
            SourceLineNo = lineNo,
            AccountCode = code,
            AccountName = string.IsNullOrWhiteSpace(name) ? code : name.Trim(),
            AccountCategoryId = catId,
            ParentAccountCode = parentCode,
            AccountLevel = level,
            IsControlAccount = level < 5,
            AllowDirectPosting = level >= 5
        };
    }

    private static CoaImportRow? ParseAmsRow(
        IReadOnlyList<string> parts,
        IReadOnlyDictionary<string, int> col,
        int lineNo,
        IReadOnlyDictionary<string, int> categoryIdByName,
        List<string> errors)
    {
        if (col.Count == 0 && parts.Count >= 7)
        {
            return ParseAmsRowByPosition(parts, lineNo, errors);
        }

        var code = Get(parts, col, "AccountCode");
        var name = Get(parts, col, "AccountName");
        var catText = Get(parts, col, "AccountCategoryID", "Account Category");
        var parentCode = Get(parts, col, "ParentAccountCode");
        var levelText = Get(parts, col, "AccountLevel", "Level #", "Level");
        var controlText = Get(parts, col, "IsControlAccount");
        var postText = Get(parts, col, "AllowDirectPosting");

        if (parts.Count < 7 && string.IsNullOrEmpty(code))
        {
            errors.Add($"Line {lineNo}: expected at least 7 columns.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            errors.Add($"Line {lineNo}: AccountCode is required.");
            return null;
        }

        int catId;
        if (int.TryParse(catText, NumberStyles.Integer, CultureInfo.InvariantCulture, out catId))
        {
            if (!categoryIdByName.Values.Contains(catId))
            {
                errors.Add($"Line {lineNo}: unknown AccountCategoryID {catId}.");
                return null;
            }
        }
        else if (!TryResolveCategoryId(code, catText, categoryIdByName, lineNo, errors, out catId))
        {
            return null;
        }

        if (!byte.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
            || level is < 1 or > 5)
        {
            errors.Add($"Line {lineNo}: invalid AccountLevel.");
            return null;
        }

        if (string.IsNullOrEmpty(parentCode) && level > 1)
            parentCode = DeriveParentAccountCode(code, level);

        var isControl = string.IsNullOrEmpty(controlText)
            ? level < 5
            : controlText is "1" or "Y" or "y" or "true" or "True";
        var allowPost = string.IsNullOrEmpty(postText)
            ? level >= 5
            : postText is not ("0" or "N" or "n" or "false" or "False");

        return new CoaImportRow
        {
            SourceLineNo = lineNo,
            AccountCode = code,
            AccountName = string.IsNullOrWhiteSpace(name) ? code : name.Trim(),
            AccountCategoryId = catId,
            ParentAccountCode = parentCode,
            AccountLevel = level,
            IsControlAccount = isControl,
            AllowDirectPosting = allowPost
        };
    }

    private static CoaImportRow? ParseAmsRowByPosition(IReadOnlyList<string> parts, int lineNo, List<string> errors)
    {
        var code = parts[0].Trim();
        var name = parts[1].Trim();
        if (!int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var catId))
        {
            errors.Add($"Line {lineNo}: invalid AccountCategoryID.");
            return null;
        }

        if (!byte.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
        {
            errors.Add($"Line {lineNo}: invalid AccountLevel.");
            return null;
        }

        var isControl = parts[5].Trim() is "1" or "Y" or "y" or "true" or "True";
        var allowPost = parts[6].Trim() is not ("0" or "N" or "n" or "false" or "False");

        return new CoaImportRow
        {
            SourceLineNo = lineNo,
            AccountCode = code,
            AccountName = name,
            AccountCategoryId = catId,
            ParentAccountCode = parts[3].Trim(),
            AccountLevel = level,
            IsControlAccount = isControl,
            AllowDirectPosting = allowPost
        };
    }

    private static bool TryResolveCategoryId(
        string accountCode,
        string categoryText,
        IReadOnlyDictionary<string, int> categoryIdByName,
        int lineNo,
        List<string> errors,
        out int categoryId)
    {
        categoryId = 0;
        if (!string.IsNullOrWhiteSpace(categoryText))
        {
            var key = categoryText.Trim();
            if (categoryIdByName.TryGetValue(key, out categoryId))
                return true;
            errors.Add($"Line {lineNo}: unknown Account Category '{key}'.");
            return false;
        }

        var m = Regex.Match(accountCode.Trim(), @"^(\d)");
        if (m.Success && CategoryIdByLeadingDigit.TryGetValue(m.Groups[1].Value, out categoryId))
            return true;

        if (categoryIdByName.TryGetValue("Asset", out categoryId))
            return true;

        errors.Add($"Line {lineNo}: cannot determine account category.");
        return false;
    }

    /// <summary>Parent code from hierarchical COA pattern (5 segments).</summary>
    public static string DeriveParentAccountCode(string accountCode, byte level)
    {
        if (level <= 1)
            return "";
        var parts = accountCode.Trim().Split('-');
        if (parts.Length != 5)
            return "";
        var idx = level - 1;
        if (idx < 1 || idx > 4)
            return "";
        parts[idx] = ZeroByIndex[idx];
        for (var i = idx + 1; i < 5; i++)
            parts[i] = ZeroByIndex[i];
        return string.Join('-', parts);
    }

    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var name = headers[i].Trim();
            if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
                map[name] = i;
        }
        return map;
    }

    private static string Get(IReadOnlyList<string> parts, IReadOnlyDictionary<string, int> col, params string[] names)
    {
        foreach (var name in names)
        {
            if (col.TryGetValue(name, out var i) && i < parts.Count)
                return parts[i].Trim();
        }
        return "";
    }

    public static List<string> SplitCsvLine(string line)
    {
        var list = new List<string>();
        var cur = new StringBuilder();
        var inQ = false;
        foreach (var ch in line)
        {
            if (ch == '"') { inQ = !inQ; continue; }
            if (ch == ',' && !inQ) { list.Add(cur.ToString()); cur.Clear(); continue; }
            cur.Append(ch);
        }
        list.Add(cur.ToString());
        return list;
    }
}
