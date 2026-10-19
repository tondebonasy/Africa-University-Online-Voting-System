# Architecture and engineering notes

## Application structure

The application uses ASP.NET MVC 5 on .NET Framework 4.7.2. Razor views render the UI; Entity Framework 6 maps the existing SQL Server schema through an EDMX model. ASP.NET Identity and OWIN manage accounts, roles and cookie authentication.

- `Controllers`: account, candidate, council, voting and administration actions.
- `Models`: generated EDMX entities and application view models.
- `Services/BallotService.cs`: common voting entry point with normal and isolated-demo persistence paths.
- `Services/DemoElectionService.cs`: guarded demo lifecycle, voting, simulations and publication.
- `Database`: clean baseline, repair script and additive demo schema.
- `Demo`: local provisioning, password setup and backup/restore tools.
- `Verification`: disposable database integration tests and demo unit/integration checks.

The frontend uses Bootstrap 5.3.8 and locally hosted Manrope fonts. `Content/Site.css` defines the university theme; `Scripts/portal.js` handles form feedback, filtering, countdowns, manifesto dialogs and vote review. jQuery remains for MVC unobtrusive validation. Bootstrap and font files are served locally. Legacy Bootstrap theme files are retained for compatibility; the active component bundle is Bootstrap 5.

## Normal election storage

The original `Candidate`, `Structure`, `CandidateStructure` and `VotingSession` entities represent nominees, positions and voting periods. The compatibility voting path retains `userVotes` and `Votes`.

**The legacy normal application associates voter usernames with candidate selections. It does not provide a secret ballot.** Its results use `userVotes`; historical imports require reconciliation before archived results can be trusted. No privacy migration of normal election records was performed.

## Isolated demonstration

The demonstration is a separate local deployment and database. It reuses the actual application and original entities, adding `DemoInstallation`, `DemoAccount`, `DemoContest`, `DemoNomination`, `DemoParticipation`, `DemoBallot` and `DemoSimulation` through [Database/Demo.sql](Database/Demo.sql).

`DemoParticipation` records only voter/contest participation and prevents duplicates with a unique key. `DemoBallot` records a random GUID, contest and nominee, without a voter ID or timestamp. Both writes occur atomically through the shared validation path. This separates application-level participation from choices; it is not cryptographic anonymity. Database administrators, transaction logs, known simulation distributions and other side channels remain outside that privacy guarantee.

Six base contests contain 19 fictional candidates. There are 200 dedicated synthetic voters and 19 eligible candidate accounts. A full preset simulation uses the 200 dedicated voters for 1,200 ballots. President and vice-president come from the highest and second-highest presidential totals. Unresolved ties block allocation/publication and require explicit approved runoffs; no alphabetical or ID-based tie-break is used.

See [Demo/README.md](Demo/README.md) for schema justification, distributions, lifecycle and rollback details.

## Security boundaries

Demo operations require development mode, an enabled demo flag, matching Identity/EDMX connections, a matching installation marker, integrated LocalDB authentication, an allowed database name and local requests. The dashboard requires `DEMO_ADMIN`; services independently verify the configured administrator. Mutations use POST and anti-forgery tokens. Voting checks nomination, eligibility, phase, dates and duplicate participation under serialized transactions.

The initializer copies only an existing authorized administrator's authentication ID, username, password hash and security stamp. It preserves that password and refuses to silently disable existing MFA. Synthetic accounts have reserved identifiers and `example.test` email addresses. They are created without shared passwords. Manual demo passwords are entered securely and hashed through Identity.

`.demo-local/` is ignored and restricted locally to the developer, SYSTEM and local administrators. Its databases, backups and copied hashes are sensitive. Source configuration disables demo mode. Normal registration/security edits and legacy administration mutations are blocked in the demo to prevent real data entering simulations. Demo and normal cookies are separated.

## Limitations and deployment considerations

- This is a portfolio/local evaluation system, not an independently certified election platform.
- The normal application does not validate registration against an institutional eligibility roster.
- Password recovery email and SMS delivery are not configured. Configure providers and recovery policy before deployment.
- Server-local election times and the server timezone must be configured consistently; browser countdowns use UTC timestamps.
- Existing dependencies include older framework/library versions. A production release needs dependency maintenance, threat modelling and independent security review.
- Further production work includes institutional identity, enforced administrator MFA, reviewed ballot privacy, auditability, accessibility and load testing.
- Previously exposed credentials must be rotated; removing them from a working tree cannot invalidate prior exposure.
- Original university images have unconfirmed rights and are excluded. The public interface uses original MIT-licensed neutral illustrations; see [ASSETS.md](ASSETS.md).

## Verification

In a Visual Studio Developer PowerShell terminal, restore and compile including Razor views:

```powershell
msbuild OnlineVotingSystem.sln /t:Restore /p:RestorePackagesConfig=true
msbuild OnlineVotingSystem.sln /p:Configuration=Debug /p:MvcBuildViews=true /verbosity:minimal
```

Run the normal regression suite using PowerShell 7 (`pwsh`):

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Verification\SmokeTest.ps1
```

The 39 checks use a disposable database/application copy on port 5098. Coverage includes registration, authentication, protected voting, atomic ballot persistence, duplicate rejection, voting periods, results, administration rendering and schedule validation. This harness uses the PowerShell 7 HTTP response API and does not support Windows PowerShell 5.1. Application provisioning and the demo test wrapper can use Windows PowerShell 5.1.

After creating your local administrator, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Verification\Test-Demo.ps1 -AdminStudentId YOUR-REGISTERED-STUDENT-ID
```

The demo suite runs 39 checks plus four separate configuration guard checks. It covers idempotent seeding, synthetic eligibility, duplicates/concurrency, lifecycle, timing, deterministic simulations, allocation, president/vice-president ties, runoffs, publication, safe reset and administrator password preservation. Temporary test databases are removed; sensitive backups remain privately under `.demo-local/`.

Local reports `Verification/last-run.txt` and `Verification/demo-last-run.txt` are generated and ignored. They are not required for a clone. [PUBLICATION.md](PUBLICATION.md) records the publication verification outcome and file inventory. Automated checks are not an exhaustive security, browser, accessibility or load audit.

