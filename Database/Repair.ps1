param([string]$Server = '(localdb)\MSSQLLocalDB')
$ErrorActionPreference = 'Stop'
$connection = New-Object System.Data.SqlClient.SqlConnection "Server=$Server;Database=online_voting_system;Integrated Security=True;Connect Timeout=10"
try {
    $connection.Open()
    $transaction = $connection.BeginTransaction()
    try {
        $command = $connection.CreateCommand()
        $command.Transaction = $transaction
        $script = Get-Content (Join-Path $PSScriptRoot 'Initialize.sql') -Raw
        $script = $script.Substring($script.IndexOf('SET ANSI_NULLS ON'))
        foreach ($batch in [regex]::Split($script, '(?im)^\s*GO\s*\r?$')) {
            if (!$batch.Trim()) { continue }
            if ($batch -match 'CREATE TABLE \[dbo\]\.\[([^\]]+)\]') {
                $batch = "IF OBJECT_ID(N'dbo.$($Matches[1])', N'U') IS NULL BEGIN`n$batch`nEND"
            } elseif ($batch -match 'CREATE (?:UNIQUE )?NONCLUSTERED INDEX \[([^\]]+)\] ON \[dbo\]\.\[([^\]]+)\]') {
                $batch = "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'$($Matches[1])' AND object_id=OBJECT_ID(N'dbo.$($Matches[2])')) BEGIN`n$batch`nEND"
            } elseif ($batch -match 'ADD\s+CONSTRAINT \[([^\]]+)\]') {
                $batch = "IF OBJECT_ID(N'dbo.$($Matches[1])') IS NULL BEGIN`n$batch`nEND"
            } elseif ($batch -match 'INSERT dbo.AspNetRoles') {
                $batch = "IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name=N'ADMIN') INSERT dbo.AspNetRoles (Id,Name) VALUES (CONVERT(nvarchar(128),NEWID()),N'ADMIN'); IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name=N'SUPER_ADMIN') INSERT dbo.AspNetRoles (Id,Name) VALUES (CONVERT(nvarchar(128),NEWID()),N'SUPER_ADMIN');"
            }
            $command.CommandText = $batch
            [void]$command.ExecuteNonQuery()
        }
        $transaction.Commit()
        Write-Host 'Schema repaired. Existing rows and tables preserved.'
    } catch { $transaction.Rollback(); throw }
} finally { $connection.Dispose() }
