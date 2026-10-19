param([string]$Server = '(localdb)\MSSQLLocalDB')
$ErrorActionPreference = 'Stop'
$connection = New-Object System.Data.SqlClient.SqlConnection "Server=$Server;Database=master;Integrated Security=True;Connect Timeout=10"
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "IF DB_ID(N'online_voting_system') IS NULL CREATE DATABASE [online_voting_system]"
    [void]$command.ExecuteNonQuery()
    $connection.ChangeDatabase('online_voting_system')
    $command.CommandText = 'SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0'
    if ([int]$command.ExecuteScalar() -eq 0) {
        $script = Get-Content (Join-Path $PSScriptRoot 'Initialize.sql') -Raw
        foreach ($batch in [regex]::Split($script, '(?im)^\s*GO\s*\r?$')) {
            if ($batch.Trim()) { $command.CommandText = $batch; [void]$command.ExecuteNonQuery() }
        }
        $connection.ChangeDatabase('online_voting_system')
    }
    $command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name IN ('VotingSession','Candidate','CandidateStructure','Structure','Votes','userVotes','AspNetUsers','AspNetRoles','AspNetUserRoles','AspNetUserClaims','AspNetUserLogins') AND schema_id=SCHEMA_ID('dbo')"
    if ([int]$command.ExecuteScalar() -ne 11) { throw 'Database is partially initialized. Back it up and inspect its schema before importing. No existing tables were changed.' }
    Write-Host 'Database ready: online_voting_system on' $Server
} finally { $connection.Dispose() }
