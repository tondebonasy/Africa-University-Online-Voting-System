param([Parameter(Mandatory=$true)][ValidatePattern('^demo\.(voter\.\d{3}|candidate\.\d{2})$')][string]$UserName)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot
$site=Join-Path $repoRoot '.demo-local/site'
[xml]$config=Get-Content (Join-Path $site 'Web.config') -Raw
$settings=@{};foreach($node in $config.configuration.appSettings.add){$settings[$node.key]=$node.value}
if($settings['Demo:Enabled'] -ne 'true' -or $settings['AppEnvironment'] -ne 'Development' -or $settings['Demo:Database'] -ne 'online_voting_system_demo'){throw 'Not an authorized local demo configuration.'}
Add-Type -Path (Join-Path $site 'bin/Microsoft.AspNet.Identity.Core.dll')
$secret=Read-Host 'Enter a new password for this synthetic demo account only (not the administrator)' -AsSecureString
$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try{
 $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
 $validator=[Microsoft.AspNet.Identity.PasswordValidator]::new();$validator.RequiredLength=6;$validator.RequireDigit=$true;$validator.RequireLowercase=$true;$validator.RequireUppercase=$true;$validator.RequireNonLetterOrDigit=$true
 $validation=$validator.ValidateAsync($plain).GetAwaiter().GetResult();if(!$validation.Succeeded){throw 'Password does not meet the displayed account requirements.'}
 $hash=[Microsoft.AspNet.Identity.PasswordHasher]::new().HashPassword($plain)
 $c=[Data.SqlClient.SqlConnection]::new('Server=(localdb)\MSSQLLocalDB;Database=online_voting_system_demo;Integrated Security=True')
 try{$c.Open();$cmd=$c.CreateCommand();$cmd.CommandText='SELECT COUNT(*) FROM dbo.DemoInstallation WHERE Id=1 AND InstanceId=@instance';[void]$cmd.Parameters.AddWithValue('@instance',[Guid]$settings['Demo:InstanceId']);if([int]$cmd.ExecuteScalar() -ne 1){throw 'Demo marker mismatch.'};$cmd.Parameters.Clear();$cmd.CommandText='UPDATE u SET PasswordHash=@hash,SecurityStamp=@stamp FROM dbo.AspNetUsers u JOIN dbo.DemoAccount a ON a.UserId=u.Id WHERE u.UserName=@name AND a.Eligible=1';[void]$cmd.Parameters.AddWithValue('@hash',$hash);[void]$cmd.Parameters.AddWithValue('@stamp',[Guid]::NewGuid().ToString());[void]$cmd.Parameters.AddWithValue('@name',$UserName);if($cmd.ExecuteNonQuery() -ne 1){throw 'Synthetic account not found.'}}finally{$c.Dispose()}
 Write-Output "Local sign-in enabled for synthetic account $UserName. No password was printed or saved to a script."
}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer);$plain=$null;$hash=$null;$secret.Dispose()}
