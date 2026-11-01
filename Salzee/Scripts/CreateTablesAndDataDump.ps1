param(
    [string]$ServerInstance = "localhost",
    [string]$DatabaseName = "PMSAbbas",
    [string]$OutputFile = "d:\PMS\PMS\PMS\Scripts\PMS_TablesAndData.sql",
    [string]$Username = "sa",
    [string]$Password = "Pakistan@786"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")
[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.ConnectionInfo")
[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.SmoExtended")
[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Management.Sdk.Sfc")

$conn = New-Object Microsoft.SqlServer.Management.Common.ServerConnection($ServerInstance, $Username, $Password)
$conn.LoginSecure = $false
$conn.TrustServerCertificate = $true
$server = New-Object Microsoft.SqlServer.Management.Smo.Server($conn)
$db = $server.Databases[$DatabaseName]

if ($null -eq $db) {
    throw "Database '$DatabaseName' not found."
}

$outDir = Split-Path -Path $OutputFile -Parent
if (-not (Test-Path $outDir)) {
    New-Item -Path $outDir -ItemType Directory -Force | Out-Null
}

@(
    "-- ================================================================",
    "-- PMS migration SQL (tables + data)",
    "-- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "-- Source: $ServerInstance / $DatabaseName",
    "-- ================================================================",
    "SET NOCOUNT ON;",
    "SET XACT_ABORT ON;",
    "GO",
    "",
    "IF DB_ID(N'$DatabaseName') IS NULL",
    "BEGIN",
    "    CREATE DATABASE [$DatabaseName];",
    "END",
    "GO",
    "USE [$DatabaseName];",
    "GO",
    ""
) | Set-Content -Path $OutputFile -Encoding UTF8

$schemaOptions = New-Object Microsoft.SqlServer.Management.Smo.ScriptingOptions
$schemaOptions.ScriptSchema = $true
$schemaOptions.ScriptData = $false
$schemaOptions.DriAll = $true
$schemaOptions.Indexes = $true
$schemaOptions.Triggers = $true
$schemaOptions.IncludeHeaders = $true
$schemaOptions.SchemaQualify = $true
$schemaOptions.AnsiPadding = $true

$dataOptions = New-Object Microsoft.SqlServer.Management.Smo.ScriptingOptions
$dataOptions.ScriptSchema = $false
$dataOptions.ScriptData = $true
$dataOptions.IncludeHeaders = $true
$dataOptions.SchemaQualify = $true
$dataOptions.AnsiPadding = $true

$schemaScripter = New-Object Microsoft.SqlServer.Management.Smo.Scripter($server)
$schemaScripter.Options = $schemaOptions
$dataScripter = New-Object Microsoft.SqlServer.Management.Smo.Scripter($server)
$dataScripter.Options = $dataOptions

$tables = $db.Tables | Where-Object { -not $_.IsSystemObject } | Sort-Object Schema, Name

Add-Content -Path $OutputFile -Value "-- ===================== TABLES ====================`r`n" -Encoding UTF8
foreach ($table in $tables) {
    Write-Host ("[Schema] [" + $table.Schema + "].[" + $table.Name + "]")
    $schemaLines = $schemaScripter.Script($table.Urn)
    foreach ($line in $schemaLines) {
        Add-Content -Path $OutputFile -Value $line -Encoding UTF8
    }
    Add-Content -Path $OutputFile -Value "`r`nGO`r`n" -Encoding UTF8
}

Add-Content -Path $OutputFile -Value "-- ====================== DATA =====================`r`n" -Encoding UTF8
Add-Content -Path $OutputFile -Value "EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';`r`nGO`r`n" -Encoding UTF8

foreach ($table in $tables) {
    $schemaName = $table.Schema
    $tableName = $table.Name
    Write-Host ("[Data]   [" + $schemaName + "].[" + $tableName + "]")

    $columnMetaQuery = @"
SELECT
    c.name AS ColumnName,
    t.name AS TypeName,
    c.is_identity AS IsIdentity,
    c.is_computed AS IsComputed,
    c.system_type_id AS SystemTypeId,
    c.column_id AS ColumnOrder
FROM sys.columns c
INNER JOIN sys.tables tb ON c.object_id = tb.object_id
INNER JOIN sys.schemas s ON tb.schema_id = s.schema_id
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE s.name = N'$schemaName' AND tb.name = N'$tableName'
ORDER BY c.column_id;
"@

    $columnMeta = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $DatabaseName -Username $Username -Password $Password -Query $columnMetaQuery
    if ($null -eq $columnMeta -or $columnMeta.Count -eq 0) { continue }

    $insertableColumns = @($columnMeta | Where-Object { $_.IsComputed -eq $false -and $_.SystemTypeId -ne 189 })
    if ($insertableColumns.Count -eq 0) { continue }

    $hasIdentity = ($insertableColumns | Where-Object { $_.IsIdentity -eq $true } | Measure-Object).Count -gt 0
    $columnList = ($insertableColumns | ForEach-Object { "[" + $_.ColumnName + "]" }) -join ", "

    $rowQuery = "SELECT * FROM [" + $schemaName + "].[" + $tableName + "];"
    $rows = @(Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $DatabaseName -Username $Username -Password $Password -Query $rowQuery)
    if ($rows.Count -eq 0) { continue }

    if ($hasIdentity) {
        Add-Content -Path $OutputFile -Value "SET IDENTITY_INSERT [$schemaName].[$tableName] ON;" -Encoding UTF8
    }

    foreach ($row in $rows) {
        $valueLiterals = @()
        foreach ($col in $insertableColumns) {
            $rawValue = $row.($col.ColumnName)
            if ($null -eq $rawValue -or $rawValue -eq [System.DBNull]::Value) {
                $valueLiterals += "NULL"
                continue
            }

            $typeName = ($col.TypeName + "").ToLowerInvariant()
            switch ($typeName) {
                { $_ -in @("nvarchar", "nchar", "ntext", "sysname") } {
                    $escaped = ($rawValue.ToString()).Replace("'", "''")
                    $valueLiterals += "N'" + $escaped + "'"
                    break
                }
                { $_ -in @("varchar", "char", "text", "xml") } {
                    $escaped = ($rawValue.ToString()).Replace("'", "''")
                    $valueLiterals += "'" + $escaped + "'"
                    break
                }
                { $_ -in @("datetime", "datetime2", "smalldatetime", "date", "time", "datetimeoffset") } {
                    $dt = [DateTime]$rawValue
                    $valueLiterals += "'" + $dt.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'"
                    break
                }
                "uniqueidentifier" {
                    $valueLiterals += "'" + $rawValue.ToString() + "'"
                    break
                }
                "bit" {
                    $valueLiterals += ($(if ([bool]$rawValue) { "1" } else { "0" }))
                    break
                }
                { $_ -in @("varbinary", "binary", "image", "timestamp", "rowversion") } {
                    $bytes = [byte[]]$rawValue
                    $hex = ($bytes | ForEach-Object { $_.ToString("X2") }) -join ""
                    $valueLiterals += "0x" + $hex
                    break
                }
                default {
                    if ($rawValue -is [string]) {
                        $escaped = $rawValue.Replace("'", "''")
                        $valueLiterals += "'" + $escaped + "'"
                    } elseif ($rawValue -is [decimal] -or $rawValue -is [double] -or $rawValue -is [single]) {
                        $valueLiterals += $rawValue.ToString([System.Globalization.CultureInfo]::InvariantCulture)
                    } else {
                        $valueLiterals += $rawValue.ToString()
                    }
                }
            }
        }

        $valuesSql = $valueLiterals -join ", "
        $insertSql = "INSERT INTO [$schemaName].[$tableName] ($columnList) VALUES ($valuesSql);"
        Add-Content -Path $OutputFile -Value $insertSql -Encoding UTF8
    }

    if ($hasIdentity) {
        Add-Content -Path $OutputFile -Value "SET IDENTITY_INSERT [$schemaName].[$tableName] OFF;" -Encoding UTF8
    }
    Add-Content -Path $OutputFile -Value "GO`r`n" -Encoding UTF8
}

Add-Content -Path $OutputFile -Value "EXEC sp_msforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';`r`nGO`r`n" -Encoding UTF8
Add-Content -Path $OutputFile -Value "-- End of file" -Encoding UTF8

Write-Host "Created: $OutputFile" -ForegroundColor Green
