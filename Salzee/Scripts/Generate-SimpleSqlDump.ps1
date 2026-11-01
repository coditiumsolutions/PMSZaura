param(
    [string]$ServerInstance = "localhost",
    [string]$DatabaseName = "PMSAbbas",
    [string]$OutputFile = "d:\PMS\PMS\PMS\Scripts\PMS_Migration_Full.sql",
    [string]$Username = "sa",
    [string]$Password = "Pakistan@786"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-ServerConnection {
    param(
        [string]$Instance,
        [string]$User,
        [string]$Pass
    )

    if ([string]::IsNullOrWhiteSpace($User)) {
        return New-Object Microsoft.SqlServer.Management.Common.ServerConnection($Instance)
    }

    $conn = New-Object Microsoft.SqlServer.Management.Common.ServerConnection($Instance, $User, $Pass)
    $conn.LoginSecure = $false
    $conn.TrustServerCertificate = $true
    return $conn
}

[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")
[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.ConnectionInfo")
[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.SmoExtended")
[void][Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Management.Sdk.Sfc")

$serverConnection = New-ServerConnection -Instance $ServerInstance -User $Username -Pass $Password
$server = New-Object Microsoft.SqlServer.Management.Smo.Server($serverConnection)
$db = $server.Databases[$DatabaseName]

if ($null -eq $db) {
    throw "Database '$DatabaseName' not found on '$ServerInstance'."
}

$outDir = Split-Path -Path $OutputFile -Parent
if (-not (Test-Path -Path $outDir)) {
    New-Item -Path $outDir -ItemType Directory -Force | Out-Null
}

@(
    "-- ====================================================================",
    "-- PMS Migration SQL (schema + data)",
    "-- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "-- Source: $ServerInstance / $DatabaseName",
    "-- ====================================================================",
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
$schemaOptions.ExtendedProperties = $true
$schemaOptions.IncludeHeaders = $true
$schemaOptions.SchemaQualify = $true
$schemaOptions.AnsiPadding = $true
$schemaOptions.NoCollation = $false
$schemaOptions.ToFileOnly = $false
$schemaOptions.WithDependencies = $false

$schemaScripter = New-Object Microsoft.SqlServer.Management.Smo.Scripter($server)
$schemaScripter.Options = $schemaOptions

$schemaUrns = New-Object System.Collections.Generic.List[Microsoft.SqlServer.Management.Sdk.Sfc.Urn]
foreach ($table in $db.Tables | Where-Object { -not $_.IsSystemObject } | Sort-Object Schema, Name) {
    $schemaUrns.Add($table.Urn)
}
foreach ($view in $db.Views | Where-Object { -not $_.IsSystemObject } | Sort-Object Schema, Name) {
    $schemaUrns.Add($view.Urn)
}
foreach ($sp in $db.StoredProcedures | Where-Object { -not $_.IsSystemObject } | Sort-Object Schema, Name) {
    $schemaUrns.Add($sp.Urn)
}

Add-Content -Path $OutputFile -Value "`r`n-- ==================== SCHEMA ====================`r`n" -Encoding UTF8
foreach ($urn in $schemaUrns) {
    $objectScript = $schemaScripter.Script($urn)
    if ($null -eq $objectScript) { continue }
    foreach ($line in $objectScript) {
        Add-Content -Path $OutputFile -Value $line -Encoding UTF8
    }
    Add-Content -Path $OutputFile -Value "`r`nGO`r`n" -Encoding UTF8
}
Add-Content -Path $OutputFile -Value "`r`nGO`r`n" -Encoding UTF8

$dataOptions = New-Object Microsoft.SqlServer.Management.Smo.ScriptingOptions
$dataOptions.ScriptSchema = $false
$dataOptions.ScriptData = $true
$dataOptions.IncludeHeaders = $true
$dataOptions.SchemaQualify = $true
$dataOptions.AnsiPadding = $true

$dataScripter = New-Object Microsoft.SqlServer.Management.Smo.Scripter($server)
$dataScripter.Options = $dataOptions

Add-Content -Path $OutputFile -Value "-- ===================== DATA =====================`r`n" -Encoding UTF8
Add-Content -Path $OutputFile -Value "EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';`r`nGO`r`n" -Encoding UTF8

$userTables = $db.Tables | Where-Object { -not $_.IsSystemObject } | Sort-Object Schema, Name
foreach ($table in $userTables) {
    $tableData = $dataScripter.Script($table.Urn)
    if ($tableData.Count -gt 0) {
        foreach ($line in $tableData) {
            Add-Content -Path $OutputFile -Value $line -Encoding UTF8
        }
        Add-Content -Path $OutputFile -Value "`r`nGO`r`n" -Encoding UTF8
    }
}

Add-Content -Path $OutputFile -Value "EXEC sp_msforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';`r`nGO`r`n" -Encoding UTF8
Add-Content -Path $OutputFile -Value "-- End of file" -Encoding UTF8

Write-Host "Created SQL dump: $OutputFile" -ForegroundColor Green
