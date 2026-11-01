using System.Text.RegularExpressions;

namespace Salzee.Services
{
    public class DatabaseSchemaService
    {
        private readonly Dictionary<string, List<string>> _tables;
        private readonly string _schemaText;

        public DatabaseSchemaService(string schemaFilePath)
        {
            _tables = new Dictionary<string, List<string>>();
            if (File.Exists(schemaFilePath))
            {
                _schemaText = File.ReadAllText(schemaFilePath);
                ParseSchema();
            }
            else
            {
                _schemaText = string.Empty;
            }
        }

        private void ParseSchema()
        {
            // Parse AI-friendly format: "TableName\n   Columns: col1, col2, col3"
            var lines = _schemaText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string? currentTable = null;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Check if line starts with a number and table name (e.g., "1. Projects")
                var tableMatch = Regex.Match(line, @"^\d+\.\s+(\w+)");
                if (tableMatch.Success)
                {
                    currentTable = tableMatch.Groups[1].Value;
                    _tables[currentTable] = new List<string>();
                }
                // Check for "Columns:" line
                else if (line.StartsWith("Columns:", StringComparison.OrdinalIgnoreCase) && currentTable != null)
                {
                    var columnsPart = line.Substring("Columns:".Length).Trim();
                    var columns = columnsPart.Split(',')
                        .Select(c => Regex.Match(c.Trim(), @"(\w+)(?:\s*\(|$)").Groups[1].Value.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                    
                    if (columns.Any())
                    {
                        _tables[currentTable] = columns;
                    }
                }
            }
        }

        public Dictionary<string, List<string>> GetAllTables()
        {
            return _tables;
        }

        public List<string> GetTableColumns(string tableName)
        {
            // Case-insensitive table lookup
            var table = _tables.Keys.FirstOrDefault(k => string.Equals(k, tableName, StringComparison.OrdinalIgnoreCase));
            return table != null ? _tables[table] : new List<string>();
        }

        public bool TableExists(string tableName)
        {
            // Case-insensitive table name lookup
            return _tables.Keys.Any(k => string.Equals(k, tableName, StringComparison.OrdinalIgnoreCase));
        }

        public bool ColumnExists(string tableName, string columnName)
        {
            // Case-insensitive table lookup, then case-insensitive column lookup
            var table = _tables.Keys.FirstOrDefault(k => string.Equals(k, tableName, StringComparison.OrdinalIgnoreCase));
            return table != null && _tables[table].Contains(columnName, StringComparer.OrdinalIgnoreCase);
        }

        public string GetSchemaSummary()
        {
            // Return the full AI-friendly schema text for better context
            if (!string.IsNullOrEmpty(_schemaText))
            {
                return _schemaText;
            }
            
            // Fallback to parsed format if text is empty
            var summary = "Database Schema:\n";
            foreach (var table in _tables)
            {
                summary += $"\nTable: {table.Key}\n";
                summary += $"Columns: {string.Join(", ", table.Value)}\n";
            }
            return summary;
        }
    }
}
