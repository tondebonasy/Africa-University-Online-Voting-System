param([Parameter(Mandatory=$true)][string]$BackupFile)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot
$root=Join-Path $repoRoot '.demo-local'
$allowed=[IO.Path]::GetFullPath((Join-Path $root 'backups'))+[IO.Path]::DirectorySeparatorChar
$file=(Resolve-Path -LiteralPath $BackupFile).Path
if(!$file.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($file) -ne '.bak'){throw 'Only private backups from this isolated demo are accepted.'}
[xml]$config=Get-Content (Join-Path $root 'site/Web.config') -Raw
$marker=[Guid](($config.configuration.appSettings.add|Where-Object key -eq 'Demo:InstanceId').value)
$c=[Data.SqlClient.SqlConnection]::new('Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True')
try{
 $c.Open();$cmd=$c.CreateCommand();$cmd.CommandTimeout=120
 $cmd.CommandText='SELECT COUNT(*) FROM online_voting_system_demo.dbo.DemoInstallation WHERE Id=1 AND InstanceId=@marker';[void]$cmd.Parameters.AddWithValue('@marker',$marker);if([int]$cmd.ExecuteScalar() -ne 1){throw 'Target demo marker mismatch. Refusing restore.'};$cmd.Parameters.Clear()
 $cmd.CommandText='RESTORE HEADERONLY FROM DISK=@path';[void]$cmd.Parameters.AddWithValue('@path',$file);$data=[Data.DataTable]::new();$adapter=[Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$adapter.Fill($data);if($data.Rows.Count -ne 1 -or $data.Rows[0].DatabaseName -ne 'online_voting_system_demo'){throw 'Backup belongs to a different database.'}
 $before=Join-Path $root ('backups/before-restore-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N')+'.bak');$cmd.Parameters.Clear();$cmd.CommandText='BACKUP DATABASE [online_voting_system_demo] TO DISK=@path WITH COPY_ONLY,INIT,CHECKSUM';[void]$cmd.Parameters.AddWithValue('@path',$before);[void]$cmd.ExecuteNonQuery()
 $pidFile=Join-Path $root 'iis.pid';if(Test-Path -LiteralPath $pidFile){$demoProcessId=[int](Get-Content $pidFile);$process=Get-CimInstance Win32_Process -Filter "ProcessId=$demoProcessId";if($process -and $process.Name -eq 'iisexpress.exe' -and $process.CommandLine.Contains((Join-Path $root 'site'))){Stop-Process -Id $demoProcessId}}
 $cmd.Parameters.Clear();$cmd.CommandText='ALTER DATABASE [online_voting_system_demo] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [online_voting_system_demo] FROM DISK=@path WITH REPLACE,RECOVERY; ALTER DATABASE [online_voting_system_demo] SET MULTI_USER;';[void]$cmd.Parameters.AddWithValue('@path',$file);[void]$cmd.ExecuteNonQuery()
 Write-Output 'Only online_voting_system_demo was restored. A pre-restore safety backup was saved. Relaunch the isolated site using Initialize-Demo.ps1.'
}finally{$c.Dispose()}
