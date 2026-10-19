# Local setup

## Prerequisites

- Windows, Visual Studio with the ASP.NET and web development workload, and IIS Express.
- .NET Framework 4.7.2 targeting pack.
- SQL Server Express LocalDB with the `(localdb)\MSSQLLocalDB` instance available.
- Windows PowerShell 5.1 for provisioning; PowerShell 7 (`pwsh`) for the regression suite; Git for source checkout.

This is an ASP.NET MVC 5 / .NET Framework application, not an ASP.NET Core application. GitHub Pages cannot run its backend.

## Restore and build

Open `OnlineVotingSystem.sln` in Visual Studio, restore the solution's NuGet packages and build it. The ignored `packages`, `bin` and `obj` directories are regenerated locally. Bootstrap 5 is vendored in the project; do not replace it with Bootstrap 3 from NuGet.

Alternatively, in a Visual Studio Developer PowerShell terminal:

```powershell
msbuild OnlineVotingSystem.sln /t:Restore /p:RestorePackagesConfig=true
msbuild OnlineVotingSystem.sln /p:Configuration=Debug /p:MvcBuildViews=true /verbosity:minimal
```

## Initialize the database

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Database\Initialize.ps1
```

The initializer creates `online_voting_system` on LocalDB and installs the schema from [Database/Initialize.sql](Database/Initialize.sql). It uses the server's default file locations. A new database contains role definitions only: no users, passwords, candidates, student records or votes.

Both connection strings in [Web.config](OnlineVotingSystem/Web.config) use Windows-integrated LocalDB authentication. There is no SQL password to configure. If choosing another SQL Server instance, update both connections consistently in a private local configuration; never commit credentials.

For SSMS, execute the complete `Database/Initialize.sql`, not a selected fragment. An already initialized database is preserved. For a partial schema, back up first and use [Database/Repair.ps1](Database/Repair.ps1). Never use the excluded historical root SQL dump for setup.

## Start the application

Set `OnlineVotingSystem` as the startup project and run through IIS Express. Use the URL Visual Studio opens; the shared project currently declares `https://localhost:44341/`. Port 5097 is a local development instance, not a required clean-clone port.

Register a new local account with a password of your choice. No default administrator or student credentials are supplied.

## Create a local administrator

In SSMS, connect to `(localdb)\MSSQLLocalDB`. Replace the placeholder below with the username/student ID of the account you just registered:

```sql
USE [online_voting_system];
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @StudentId nvarchar(256) = N'YOUR-REGISTERED-STUDENT-ID';
DECLARE @UserId nvarchar(128) = (SELECT Id FROM dbo.AspNetUsers WHERE UserName = @StudentId);
IF @UserId IS NULL THROW 50000, 'Register the account first.', 1;
DECLARE @RoleId nvarchar(128) = (SELECT Id FROM dbo.AspNetRoles WHERE Name = N'ADMIN');
IF @RoleId IS NULL
BEGIN
    SET @RoleId = CONVERT(nvarchar(128), NEWID());
    INSERT dbo.AspNetRoles (Id, Name) VALUES (@RoleId, N'ADMIN');
END;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    INSERT dbo.AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
COMMIT;
```

Sign out and sign in again to refresh role claims. Assign roles only to accounts you control for local evaluation.

## Prepare the isolated demonstration

After building and creating your administrator:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Demo\Initialize-Demo.ps1 -AdminStudentId YOUR-REGISTERED-STUDENT-ID
```

Open `http://localhost:5099/Demo` and use that same locally chosen administrator password. The initializer copies only that administrator's authentication material into the protected local demo deployment; no real student list is copied. Demo data, backups and deployment files remain in ignored `.demo-local/`.

Follow [Demo/README.md](Demo/README.md) for the complete election walkthrough, synthetic voter login provisioning, reset and restoration. Consult [ARCHITECTURE.md](ARCHITECTURE.md) before interpreting ballot privacy or deploying beyond local evaluation.

## Troubleshooting

- Missing package/build imports: restore NuGet packages, then rebuild.
- SQL connection failure: confirm LocalDB is installed and start the instance with `sqllocaldb start MSSQLLocalDB`.
- Missing tables: execute the full initializer; the application and Identity connections must target the same database.
- Missing targeting assemblies or web targets: install the specified Visual Studio workload and targeting pack.
- Demo administrator not found: register and grant ADMIN in the normal local database first; pass your own username.
- Port already in use: stop the corresponding local development instance. Do not terminate unrelated processes.

See [GITHUB.md](GITHUB.md) for publication precautions and [ARCHITECTURE.md](ARCHITECTURE.md#verification) for test commands.


## Public images

The landing/account backgrounds and portal emblem are bundled neutral SVG illustrations. A clone needs no private image files or external image service. Original university images are kept only in the developer's ignored private asset folder; see [ASSETS.md](ASSETS.md) for the public image licence and inventory.
