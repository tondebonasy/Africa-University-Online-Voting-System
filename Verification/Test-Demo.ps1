param([Parameter(Mandatory=$true)][string]$AdminStudentId)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot
$run=[Guid]::NewGuid().ToString('N').Substring(0,10)
$dbName='online_voting_system_demo_test_'+$run
$runRoot=Join-Path $repoRoot ('.demo-local/'+$dbName)
try{
 & (Join-Path $repoRoot 'Demo/Initialize-Demo.ps1') -AdminStudentId $AdminStudentId -DatabaseName $dbName -NoLaunch
 if($LASTEXITCODE -ne 0){throw 'Test installation failed'}
 $bin=Join-Path $runRoot 'site/bin'
 $csc='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 & $csc /nologo /target:exe ("/out:"+(Join-Path $bin 'Demo.Tests.exe')) ("/r:"+(Join-Path $bin 'OnlineVotingSystem.dll')) ("/r:"+(Join-Path $bin 'System.Web.Mvc.dll')) /r:System.Configuration.dll /r:System.Data.dll /r:System.Web.dll (Join-Path $PSScriptRoot 'DemoTests.cs')
 if($LASTEXITCODE -ne 0){throw 'Demo test compilation failed'}
 Copy-Item (Join-Path $bin 'Demo.Tool.exe.config') (Join-Path $bin 'Demo.Tests.exe.config') -Force
 $output=& (Join-Path $bin 'Demo.Tests.exe') 2>&1
 $code=$LASTEXITCODE
 $output|ForEach-Object{Write-Output $_}
 if($code -ne 0){throw 'Demo unit/integration checks failed.'}
 # Verify fail-closed guards in separate processes; no unsafe target is opened.
 foreach($guard in @('Disabled','ProductionEnvironment','RealDatabase','MismatchedMarker')){
  $guardExe=Join-Path $bin ('Guard-'+$guard+'.exe');Copy-Item (Join-Path $bin 'Demo.Tool.exe') $guardExe
  [xml]$cfg=Get-Content (Join-Path $bin 'Demo.Tool.exe.config') -Raw
  if($guard -eq 'Disabled'){($cfg.configuration.appSettings.add|Where-Object key -eq 'Demo:Enabled').value='false'}
  if($guard -eq 'ProductionEnvironment'){($cfg.configuration.appSettings.add|Where-Object key -eq 'AppEnvironment').value='Production'}
  if($guard -eq 'RealDatabase'){($cfg.configuration.appSettings.add|Where-Object key -eq 'Demo:Database').value='online_voting_system';foreach($entry in $cfg.configuration.connectionStrings.add){$entry.connectionString=$entry.connectionString.Replace($dbName,'online_voting_system')}}
  if($guard -eq 'MismatchedMarker'){($cfg.configuration.appSettings.add|Where-Object key -eq 'Demo:InstanceId').value=[Guid]::NewGuid().ToString()}
  $cfg.Save($guardExe+'.config')
  $previous=$ErrorActionPreference;$ErrorActionPreference='Continue';$null=& $guardExe status 2>&1;$guardCode=$LASTEXITCODE;$ErrorActionPreference=$previous
  if($guardCode -eq 0){throw "Fail-closed guard failed: $guard"}
  Write-Output "PASS: configuration guard $guard rejects demo operations"
 }
 Set-Content (Join-Path $PSScriptRoot 'demo-last-run.txt') (($output -join "`r`n")+"`r`nFour fail-closed configuration guards passed.`r`nAll data synthetic; test database removed by cleanup. Initial and reset backups retained privately under .demo-local.")
}finally{
 if($dbName -match '^online_voting_system_demo_test_[a-f0-9]{10}$'){
  $c=[Data.SqlClient.SqlConnection]::new('Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True')
  try{$c.Open();$cmd=$c.CreateCommand();$cmd.CommandText="IF DB_ID(N'$dbName') IS NOT NULL BEGIN ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$dbName]; END";[void]$cmd.ExecuteNonQuery()}finally{$c.Dispose()}
 }
}
