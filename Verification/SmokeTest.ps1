# Runs against a disposable copy of the application and a disposable database.
param([int]$Port = 5098, [switch]$BrowserCheck)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$run = [Guid]::NewGuid().ToString('N').Substring(0,10)
$dbName = 'ovs_smoke_' + $run
$site = Join-Path $PSScriptRoot ('scratch_' + $run)
$base = "http://localhost:$Port"
$checks = [System.Collections.Generic.List[string]]::new()
$serverProcess = $null
$connection = [System.Data.SqlClient.SqlConnection]::new('Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;Connect Timeout=10')
function Check($Condition, [string]$Name) {
    if (!$Condition) { throw "FAILED: $Name" }
    $checks.Add($Name)
    Write-Host "PASS: $Name"
}
function Sql([string]$Text) {
    $cmd=$connection.CreateCommand(); $cmd.CommandText=$Text
    try { return $cmd.ExecuteScalar() } finally { $cmd.Dispose() }
}
function Token($Page) {
    $match=[regex]::Match($Page.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (!$match.Success) { throw 'Missing anti-forgery token in form' }
    return [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}
function Page([string]$Path, $Session) { Invoke-WebRequest ($base+$Path) -WebSession $Session -UseBasicParsing -TimeoutSec 60 }
function Post([string]$Path, $Body, $Session) { Invoke-WebRequest ($base+$Path) -Method Post -Body $Body -WebSession $Session -UseBasicParsing -TimeoutSec 60 }
try {
    $connection.Open()
    $script=(Get-Content (Join-Path $root 'Database/Initialize.sql') -Raw).Replace('online_voting_system',$dbName)
    foreach($batch in [regex]::Split($script,'(?im)^\s*GO\s*\r?$')) { if($batch.Trim()){ [void](Sql $batch) } }
    $connection.ChangeDatabase($dbName)
    New-Item -ItemType Directory -Path $site | Out-Null
    foreach($folder in @('bin','Views','Content','Scripts')) { Copy-Item -LiteralPath (Join-Path $root "OnlineVotingSystem/$folder") -Destination $site -Recurse }
    Copy-Item -LiteralPath (Join-Path $root 'OnlineVotingSystem/Global.asax') -Destination $site
    $config=(Get-Content (Join-Path $root 'OnlineVotingSystem/Web.config') -Raw).Replace('online_voting_system',$dbName)
    Set-Content (Join-Path $site 'Web.config') $config
    $serverProcess=Start-Process 'C:\Program Files\IIS Express\iisexpress.exe' -ArgumentList @("/path:$site","/port:$Port",'/systray:false') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $site 'iis.log') -RedirectStandardError (Join-Path $site 'iis-error.log')
    $student=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $anon=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $ready=$false
    for($attempt=0;$attempt -lt 20;$attempt++) {
        try { $landingResult=Page '/' $anon; $ready=$true; break } catch { Start-Sleep -Seconds 2 }
    }
    if(!$ready){throw 'Disposable test application did not start'}
    Check ($landingResult.StatusCode -eq 200 -and $landingResult.Content.Contains('Africa University')) 'Landing page renders against a clean database'
    foreach($path in @('/Account/Login','/Account/Register','/Account/ForgotPassword','/Home/About','/Home/Contact','/Content/images/election-mark.svg','/Content/images/campus-aerial.svg','/Content/images/campus-garden.svg','/Scripts/portal.js')) {
        $r=Page $path $anon; Check ($r.StatusCode -eq 200) "$path responds"
    }
    $r=Page '/Candidates' $anon; Check ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Anonymous management access redirects to login'
    $username='smoke_'+$run
    $password='Aa!7'+[Guid]::NewGuid().ToString('N')
    $r=Page '/Account/Register' $student
    $body=@{__RequestVerificationToken=(Token $r);StudentNumber=$username;Email="$username@example.test";PhoneNumber='+263771234567';Password=$password;ConfirmPassword='different'}
    $r=Post '/Account/Register' $body $student
    Check ($r.Content.Contains('do not match') -and [int](Sql "SELECT COUNT(*) FROM dbo.AspNetUsers WHERE UserName=N'$username'") -eq 0) 'Invalid registration does not create an account'
    $r=Page '/Account/Register' $student; $body.__RequestVerificationToken=Token $r; $body.ConfirmPassword=$password
    $r=Post '/Account/Register' $body $student
    Check ($r.Content.Contains('Sign out')) 'Valid registration creates and signs in a student'
    $userId=[string](Sql "SELECT Id FROM dbo.AspNetUsers WHERE UserName=N'$username'")
    Check (![string]::IsNullOrEmpty($userId)) 'Registered student persisted'
    # Voting fixtures exist only in the disposable test database.
    $position=[int](Sql "INSERT dbo.Structure (name,rank) VALUES (N'Test president',1); SELECT CAST(SCOPE_IDENTITY() AS int)")
    $candidate=[int](Sql "INSERT dbo.Candidate (name,userAccountId,manifesto) VALUES (N'Test candidate',N'$userId',N'/Uploads/manifesto.pdf'); SELECT CAST(SCOPE_IDENTITY() AS int)")
    $nomination=[int](Sql "INSERT dbo.CandidateStructure (candidateId,structureId) VALUES ($candidate,$position); SELECT CAST(SCOPE_IDENTITY() AS int)")
    $election=[int](Sql "INSERT dbo.VotingSession (createdBy,startDay,endDate,status) VALUES (N'smoke',DATEADD(hour,-1,GETDATE()),DATEADD(hour,1,GETDATE()),N'Open'); SELECT CAST(SCOPE_IDENTITY() AS int)")
    $r=Page '/Home/ViewCandidates' $student
    Check ($r.Content.Contains('Test candidate') -and $r.Content.Contains('/Uploads/manifesto.pdf') -and $r.Content.Contains('candidate-search')) 'Candidate ballot, manifesto link and live-search control render'
    $ballotToken=Token $r
    $blocked=$false
    try { [void](Post '/Votes/Vote' @{id=$nomination} $student) } catch { $blocked=$true }
    Check ($blocked -and [int](Sql 'SELECT COUNT(*) FROM dbo.userVotes') -eq 0) 'Ballots without anti-forgery tokens are rejected'
    $r=Post '/Votes/Vote' @{id=$nomination;__RequestVerificationToken=$ballotToken} $student
    Check ($r.Content.Contains('Your vote has been recorded') -and [int](Sql 'SELECT COUNT(*) FROM dbo.userVotes') -eq 1 -and [int](Sql 'SELECT SUM(voteCount) FROM dbo.Votes') -eq 1) 'Vote and tally are committed together'
    $r=Post '/Votes/Vote' @{id=$nomination;__RequestVerificationToken=$ballotToken} $student
    Check ($r.Content.Contains('already voted') -and [int](Sql 'SELECT COUNT(*) FROM dbo.userVotes') -eq 1 -and [int](Sql 'SELECT SUM(voteCount) FROM dbo.Votes') -eq 1) 'Duplicate ballot is rejected without changing tally'
    $r=Page '/Home/ViewResults' $student
    Check ($r.Content.Contains('Test president') -and $r.Content.Contains('Test candidate') -and $r.Content.Contains('100.0')) 'Results show the recorded ballot under its position'
    [void](Sql "UPDATE dbo.VotingSession SET startDay=DATEADD(day,1,GETDATE()),endDate=DATEADD(day,2,GETDATE()) WHERE Id=$election")
    $r=Post '/Votes/Vote' @{id=$nomination;__RequestVerificationToken=$ballotToken} $student
    Check ($r.Content.Contains('no election open') -and [int](Sql 'SELECT COUNT(*) FROM dbo.userVotes') -eq 1) 'Future elections reject voting'
    [void](Sql "UPDATE dbo.VotingSession SET startDay=DATEADD(day,-2,GETDATE()),endDate=DATEADD(day,-1,GETDATE()) WHERE Id=$election")
    $r=Post '/Votes/Vote' @{id=$nomination;__RequestVerificationToken=$ballotToken} $student
    Check ($r.Content.Contains('no election open')) 'Closed elections reject voting'
    # A fresh identity is used solely to check the administrative views in this isolated database.
    [void](Sql "INSERT dbo.AspNetUserRoles (UserId,RoleId) SELECT N'$userId',Id FROM dbo.AspNetRoles WHERE Name=N'ADMIN'")
    $r=Page '/' $student; $r=Post '/Account/LogOff' @{__RequestVerificationToken=(Token $r)} $student
    $r=Page '/Account/Login' $student
    $r=Post '/Account/Login' @{__RequestVerificationToken=(Token $r);StudentNumber=$username;Password='Incorrect!12'} $student
    Check ($r.Content.Contains('Invalid login attempt')) 'Incorrect password is rejected'
    $r=Page '/Account/Login' $student
    $r=Post '/Account/Login' @{__RequestVerificationToken=(Token $r);StudentNumber=$username;Password=$password} $student
    Check ($r.Content.Contains('Administration')) 'Correct password signs in and loads assigned administrator role'
    foreach($path in @('/Home/ViewCouncilStructures','/Manage','/Candidates','/Candidates/Create','/CandidateStructures','/CandidateStructures/Create','/Structures','/Structures/Create','/VotingSessions','/VotingSessions/Create','/Roles','/Roles/Create','/Users')) {
        $r=Page $path $student; Check ($r.StatusCode -eq 200 -and !$r.Content.Contains('Server Error')) "$path renders for authorized account"
    }
    $r=Page '/VotingSessions/Create' $student
    $r=Post '/VotingSessions/Create' @{__RequestVerificationToken=(Token $r);startDay='2030-01-02';endDate='2030-01-01';status='Open'} $student
    Check ($r.Content.Contains('end date after the start')) 'Invalid election date range is rejected'
    $r=Page '/VotingSessions/Create' $student
    $r=Post '/VotingSessions/Create' @{__RequestVerificationToken=(Token $r);startDay='2030-01-01';endDate='2030-01-02';status='Open'} $student
    Check ([int](Sql 'SELECT COUNT(*) FROM dbo.VotingSession') -eq 2) 'Administrator can schedule an election'
    $r=Page '/' $student; $r=Post '/Account/LogOff' @{__RequestVerificationToken=(Token $r)} $student
    Check (!$r.Content.Contains('Sign out')) 'Sign out clears authentication'
    if ($BrowserCheck) {
        [void](Sql "UPDATE dbo.VotingSession SET startDay=DATEADD(hour,-1,GETDATE()),endDate=DATEADD(hour,1,GETDATE()) WHERE Id=$election")
        $handoffPath=Join-Path $PSScriptRoot 'browser-check.json'
        @{Url=$base;User=$username;Password=$password;UserId=$userId;Run=$run} | ConvertTo-Json | Set-Content $handoffPath
        Write-Host 'Integration checks passed. Disposable browser site ready on port' $Port
        $deadline=[DateTime]::UtcNow.AddMinutes(5)
        while((Test-Path -LiteralPath $handoffPath) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 1 }
        if(Test-Path -LiteralPath $handoffPath){Remove-Item -LiteralPath $handoffPath -Force}
    }
    $result="Passed $($checks.Count) integration checks in disposable database $dbName. Production database was not modified. Test database and application copy were removed."
    Set-Content (Join-Path $PSScriptRoot 'last-run.txt') ($result+"`r`n"+($checks -join "`r`n"))
    Write-Host $result
} finally {
    if($serverProcess -and !$serverProcess.HasExited){ Stop-Process -Id $serverProcess.Id -Force }
    if($connection.State -eq 'Open') {
        $connection.ChangeDatabase('master')
        if($dbName -match '^ovs_smoke_[a-f0-9]{10}$') { [void](Sql "IF DB_ID(N'$dbName') IS NOT NULL BEGIN ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$dbName]; END") }
    }
    $connection.Dispose()
    $resolvedSite=[System.IO.Path]::GetFullPath($site)
    $allowed=[System.IO.Path]::GetFullPath($PSScriptRoot)+[System.IO.Path]::DirectorySeparatorChar
    if($resolvedSite.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedSite)) { for($cleanupAttempt=0;$cleanupAttempt -lt 5;$cleanupAttempt++) {
            try { Remove-Item -LiteralPath $resolvedSite -Recurse -Force; break }
            catch { if($cleanupAttempt -eq 4){throw}; Start-Sleep -Seconds 2 }
        }
    }
}



