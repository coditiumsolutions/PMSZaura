using System.Text.RegularExpressions;

namespace Salzee.Services
{
    public class SQLQueryValidatorService
    {
        private readonly DatabaseSchemaService _schemaService;

        public SQLQueryValidatorService(DatabaseSchemaService schemaService)
        {
            _schemaService = schemaService;
        }

        public (bool IsValid, string CorrectedQuery, string ErrorMessage) ValidateAndCorrectQuery(string sqlQuery)
        {
            try
            {
                // Extract table names from SQL query (handles aliases: FROM TableName alias)
                // Pattern matches: FROM TableName, FROM TableName alias, JOIN TableName, etc.
                var tablePattern = @"(?:FROM|JOIN|UPDATE|INTO)\s+(\w+)(?:\s+\w+)?";
                var tableMatches = Regex.Matches(sqlQuery, tablePattern, RegexOptions.IgnoreCase);
                
                var tablesInQuery = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match match in tableMatches)
                {
                    if (match.Groups[1].Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        var tableName = match.Groups[1].Value;
                        // Skip SQL keywords that might be matched incorrectly
                        if (!new[] { "SELECT", "WHERE", "GROUP", "ORDER", "HAVING", "ON", "AS" }
                            .Contains(tableName.ToUpper()))
                        {
                            tablesInQuery.Add(tableName);
                        }
                    }
                }

                // Validate tables exist (case-insensitive)
                var invalidTables = new List<string>();
                foreach (var table in tablesInQuery)
                {
                    if (!_schemaService.TableExists(table))
                    {
                        invalidTables.Add(table);
                    }
                }

                if (invalidTables.Any())
                {
                    return (false, sqlQuery, $"Invalid table(s): {string.Join(", ", invalidTables)}");
                }

                // Extract column names from SELECT clause (skip aliases and aggregate functions)
                var selectPattern = @"SELECT\s+(.*?)\s+FROM";
                var selectMatch = Regex.Match(sqlQuery, selectPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                
                if (selectMatch.Success)
                {
                    var columnsPart = selectMatch.Groups[1].Value;
                    if (!columnsPart.Contains("*"))
                    {
                        // Remove aliases (everything after AS keyword)
                        var withoutAliases = Regex.Replace(columnsPart, @"\s+AS\s+\w+", "", RegexOptions.IgnoreCase);
                        
                        // Extract aggregate functions and their arguments: COUNT(*), COUNT(column), SUM(column), etc.
                        // Pattern matches: COUNT(*), COUNT(column), SUM(column), AVG(table.column)
                        var aggregatePattern = @"(COUNT|SUM|AVG|MAX|MIN)\s*\(([^)]+)\)";
                        var aggregateMatches = Regex.Matches(withoutAliases, aggregatePattern, RegexOptions.IgnoreCase);
                        
                        var columnsToValidate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        
                        // Extract columns from aggregate functions (skip * and expressions)
                        foreach (Match aggMatch in aggregateMatches)
                        {
                            var arg = aggMatch.Groups[2].Value.Trim();
                            // Skip * and complex expressions
                            if (arg != "*" && !arg.Contains("+") && !arg.Contains("-") && !arg.Contains("*") && !arg.Contains("/"))
                            {
                                // Handle qualified names (table.column)
                                var qualifiedMatch = Regex.Match(arg, @"(\w+)\.(\w+)|(\w+)");
                                if (qualifiedMatch.Success)
                                {
                                    if (qualifiedMatch.Groups[2].Success)
                                    {
                                        // table.column format
                                        columnsToValidate.Add(qualifiedMatch.Groups[2].Value);
                                    }
                                    else if (qualifiedMatch.Groups[3].Success)
                                    {
                                        // Just column name
                                        columnsToValidate.Add(qualifiedMatch.Groups[3].Value);
                                    }
                                }
                            }
                        }
                        
                        // Remove aggregate functions from the string to find regular columns
                        var withoutAggregates = Regex.Replace(withoutAliases, aggregatePattern, "", RegexOptions.IgnoreCase);
                        
                        // Extract regular column references (handle table.column and plain column)
                        var columnRefPattern = @"\b(\w+)\.(\w+)\b|\b(\w+)\b";
                        var columnMatches = Regex.Matches(withoutAggregates, columnRefPattern);
                        
                        foreach (Match colMatch in columnMatches)
                        {
                            var columnName = colMatch.Groups[2].Success ? colMatch.Groups[2].Value : 
                                           colMatch.Groups[3].Success ? colMatch.Groups[3].Value : null;
                            
                            if (string.IsNullOrWhiteSpace(columnName))
                                continue;
                            
                            // Skip SQL keywords
                            if (new[] { "SELECT", "DISTINCT", "TOP", "COUNT", "SUM", "AVG", "MAX", "MIN", "AS", "FROM", "WHERE", "JOIN", "ON", "GROUP", "ORDER", "BY", "HAVING" }
                                .Contains(columnName.ToUpper()))
                                continue;
                            
                            columnsToValidate.Add(columnName);
                        }
                        
                        // Validate columns exist in tables
                        var invalidColumns = new List<string>();
                        foreach (var columnName in columnsToValidate)
                        {
                            bool columnExists = false;
                            foreach (var table in tablesInQuery)
                            {
                                if (_schemaService.ColumnExists(table, columnName))
                                {
                                    columnExists = true;
                                    break;
                                }
                            }

                            if (!columnExists)
                            {
                                invalidColumns.Add(columnName);
                            }
                        }

                        if (invalidColumns.Any())
                        {
                            return (false, sqlQuery, $"Invalid column(s): {string.Join(", ", invalidColumns)}");
                        }
                    }
                }

                return (true, sqlQuery, "Query is valid");
            }
            catch (Exception ex)
            {
                return (false, sqlQuery, $"Validation error: {ex.Message}");
            }
        }
    }
}
