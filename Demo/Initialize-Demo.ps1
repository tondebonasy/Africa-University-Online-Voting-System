param([Parameter(Mandatory=$true)][string]$AdminStudentId,[string]$DatabaseName='online_voting_system_demo',[switch]$NoLaunch,[switch]$NoSeed,[int]$Port=5099)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot
if($DatabaseName -ne 'online_voting_system_demo' -and $DatabaseName -notmatch '^online_voting_system_demo_test_[a-f0-9]{10}$'){throw 'Unsafe demo database name.'}
$localRoot=Join-Path $repoRoot '.demo-local'
$runRoot=if($DatabaseName -eq 'online_voting_system_demo'){$localRoot}else{Join-Path $localRoot $DatabaseName}
$siteRoot=Join-Path $runRoot 'site'
$backupRoot=Join-Path $runRoot 'backups'
New-Item -ItemType Directory -Force $siteRoot,$backupRoot | Out-Null
# Keep copied authentication backups/configuration private to this developer and OS administrators.
$userGrant='*'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value+':(OI)(CI)F'
$null=& icacls.exe $localRoot /inheritance:r /grant:r $userGrant '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F'
if($LASTEXITCODE -ne 0){throw 'Could not secure the private local demo directory. No database changes were made.'}
# Stop only this installation's previous IIS Express instance before replacing its files.
$pidPath=Join-Path $runRoot 'iis.pid'
if(Test-Path -LiteralPath $pidPath){
 $oldDemoPid=[int](Get-Content $pidPath)
 $oldProcess=Get-CimInstance Win32_Process -Filter "ProcessId=$oldDemoPid"
 if($oldProcess -and $oldProcess.Name -eq 'iisexpress.exe' -and $oldProcess.CommandLine.Contains($siteRoot)){Stop-Process -Id $oldDemoPid;Wait-Process -Id $oldDemoPid -Timeout 5 -ErrorAction SilentlyContinue}
}
$source=[System.Data.SqlClient.SqlConnection]::new('Server=(localdb)\MSSQLLocalDB;Database=online_voting_system;Integrated Security=True;Connect Timeout=10')
try{
 $source.Open();$cmd=$source.CreateCommand();$cmd.CommandText="SELECT u.Id,u.UserName,u.PasswordHash,u.SecurityStamp,u.TwoFactorEnabled FROM dbo.AspNetUsers u WHERE u.UserName=@name AND EXISTS(SELECT 1 FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE ur.UserId=u.Id AND r.Name IN(N'ADMIN',N'SUPER_ADMIN'))";[void]$cmd.Parameters.AddWithValue('@name',$AdminStudentId)
 $table=[Data.DataTable]::new();$adapter=[Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$adapter.Fill($table)
 if($table.Rows.Count -ne 1 -or $table.Rows[0].IsNull('PasswordHash')){throw 'The supplied student ID must be an existing password-based administrator. No password will be changed.'}
 if([bool]$table.Rows[0].TwoFactorEnabled){throw 'The existing administrator requires two-factor authentication. Configure the same provider for the isolated demo; setup will not disable it.'}
 $admin=$table.Rows[0]
}finally{$source.Dispose()}
$c=[Data.SqlClient.SqlConnection]::new('Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;Connect Timeout=10')
try{
 $c.Open();$cmd=$c.CreateCommand();$cmd.CommandTimeout=120
 $cmd.CommandText="IF DB_ID(N'$DatabaseName') IS NULL CREATE DATABASE [$DatabaseName]";[void]$cmd.ExecuteNonQuery();$c.ChangeDatabase($DatabaseName)
 $cmd.CommandText='SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0';$tableCount=[int]$cmd.ExecuteScalar()
 if($tableCount -gt 0){$cmd.CommandText="SELECT COUNT(*) FROM sys.tables WHERE name='DemoInstallation'";if([int]$cmd.ExecuteScalar() -ne 1){throw 'Existing target has no demo marker. Refusing to change it. Restore the initial backup or choose a fresh isolated test database.'}}
 $backup=Join-Path $backupRoot ($DatabaseName+'-before-setup-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N')+'.bak')
 $cmd.CommandText="BACKUP DATABASE [$DatabaseName] TO DISK=@path WITH COPY_ONLY,INIT,CHECKSUM";[void]$cmd.Parameters.AddWithValue('@path',$backup);[void]$cmd.ExecuteNonQuery();$cmd.Parameters.Clear()
 if($tableCount -eq 0){$schema=(Get-Content (Join-Path $repoRoot 'Database/Initialize.sql') -Raw).Replace('online_voting_system',$DatabaseName);foreach($batch in [regex]::Split($schema,'(?im)^\s*GO\s*\r?$')){if($batch.Trim()){$cmd.CommandText=$batch;[void]$cmd.ExecuteNonQuery()}};$c.ChangeDatabase($DatabaseName)}
 foreach($batch in [regex]::Split((Get-Content (Join-Path $repoRoot 'Database/Demo.sql') -Raw),'(?im)^\s*GO\s*\r?$')){if($batch.Trim()){$cmd.CommandText=$batch;[void]$cmd.ExecuteNonQuery()}}
 $tx=$c.BeginTransaction();$cmd.Transaction=$tx
 try{
  $cmd.CommandText='SELECT InstanceId FROM dbo.DemoInstallation WHERE Id=1';$instance=$cmd.ExecuteScalar()
  if($null -eq $instance){$instance=[Guid]::NewGuid()}else{$cmd.CommandText='SELECT AdminUserId FROM dbo.DemoInstallation WHERE Id=1';if([string]$cmd.ExecuteScalar() -ne [string]$admin.Id){throw 'This demo belongs to a different configured administrator.'}}
  $cmd.Parameters.Clear();foreach($name in @('Id','UserName','PasswordHash','SecurityStamp')){[void]$cmd.Parameters.AddWithValue('@'+$name,$admin[$name])};[void]$cmd.Parameters.AddWithValue('@instance',$instance)
  $cmd.CommandText=@"
IF NOT EXISTS(SELECT 1 FROM dbo.AspNetUsers WHERE Id=@Id)
 INSERT dbo.AspNetUsers(Id,UserName,PasswordHash,SecurityStamp,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount) VALUES(@Id,@UserName,@PasswordHash,@SecurityStamp,0,0,0,1,0);
IF NOT EXISTS(SELECT 1 FROM dbo.AspNetRoles WHERE Name=N'DEMO_ADMIN') INSERT dbo.AspNetRoles(Id,Name) VALUES(CONVERT(nvarchar(128),NEWID()),N'DEMO_ADMIN');
INSERT dbo.AspNetUserRoles(UserId,RoleId) SELECT @Id,r.Id FROM dbo.AspNetRoles r WHERE r.Name IN(N'ADMIN',N'DEMO_ADMIN') AND NOT EXISTS(SELECT 1 FROM dbo.AspNetUserRoles ur WHERE ur.UserId=@Id AND ur.RoleId=r.Id);
IF NOT EXISTS(SELECT 1 FROM dbo.DemoInstallation WHERE Id=1) INSERT dbo.DemoInstallation(Id,InstanceId,AdminUserId,Seeded) VALUES(1,@instance,@Id,0);
"@
  [void]$cmd.ExecuteNonQuery();$tx.Commit()
 }catch{if($tx.Connection){$tx.Rollback()};throw}
}finally{$c.Dispose()}
# One application, deployed as a separate local instance; integrated authentication only.
foreach($folder in @('bin','Views','Content','Scripts')){Copy-Item -LiteralPath (Join-Path $repoRoot ('OnlineVotingSystem/'+$folder)) -Destination $siteRoot -Recurse -Force}
Copy-Item -LiteralPath (Join-Path $repoRoot 'OnlineVotingSystem/Global.asax') -Destination $siteRoot -Force
[xml]$config=Get-Content (Join-Path $repoRoot 'OnlineVotingSystem/Web.config') -Raw
foreach($item in $config.configuration.connectionStrings.add){$item.connectionString=$item.connectionString.Replace('online_voting_system',$DatabaseName)}
$settings=@{'Demo:Enabled'='true';'AppEnvironment'='Development';'Demo:Database'=$DatabaseName;'Demo:InstanceId'=$instance.ToString();'Demo:AdminUserId'=[string]$admin.Id;'Demo:AdminUserName'=[string]$admin.UserName;'Demo:RootDirectory'=$runRoot;'Demo:BackupDirectory'=$backupRoot}
foreach($entry in $settings.GetEnumerator()){$node=$config.configuration.appSettings.add|Where-Object key -eq $entry.Key;if(!$node){$node=$config.CreateElement('add');$node.SetAttribute('key',$entry.Key);[void]$config.configuration.appSettings.AppendChild($node)};$node.SetAttribute('value',$entry.Value)}
$config.Save((Join-Path $siteRoot 'Web.config'))
# A separate cookie prevents the real site and demo sharing authentication on localhost.
$authFile=Join-Path $siteRoot 'bin/OnlineVotingSystem.dll'
$binRoot=Join-Path $siteRoot 'bin'
$csc='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:exe ("/out:"+(Join-Path $binRoot 'Demo.Tool.exe')) ("/r:"+$authFile) /r:System.Configuration.dll (Join-Path $PSScriptRoot 'Tool.cs')
if($LASTEXITCODE -ne 0){throw 'Demo tool compilation failed'}
[xml]$toolConfig='<configuration><appSettings/><connectionStrings/></configuration>'
foreach($section in @('appSettings','connectionStrings')){[void]$toolConfig.SelectSingleNode("/configuration").ReplaceChild($toolConfig.ImportNode($config.configuration.$section,$true),$toolConfig.SelectSingleNode("/configuration/"+$section))}
$toolConfig.Save((Join-Path $binRoot 'Demo.Tool.exe.config'))
if(!$NoSeed){& (Join-Path $binRoot 'Demo.Tool.exe') seed;if($LASTEXITCODE -ne 0){throw 'Demo seeding failed; inspect the initial backup for rollback.'}}
@{Site=$siteRoot;Database=$DatabaseName;Port=$Port;BackupDirectory=$backupRoot;AdminStudentId=$AdminStudentId} | ConvertTo-Json | Set-Content (Join-Path $runRoot 'installation.json')
if(!$NoLaunch){$process=Start-Process 'C:\Program Files\IIS Express\iisexpress.exe' -ArgumentList @("/path:$siteRoot","/port:$Port",'/systray:false') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runRoot 'iis.log') -RedirectStandardError (Join-Path $runRoot 'iis-error.log');Set-Content (Join-Path $runRoot 'iis.pid') $process.Id}
Write-Output "DEMO isolated site ready at http://localhost:$Port/Demo. Sign in with the existing administrator's unchanged password."
Write-Output "Backups are stored locally under $backupRoot"





