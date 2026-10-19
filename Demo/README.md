# Development-only election demonstration

This is the existing MVC 5 application running as a separate, local instance. The real `online_voting_system` database is never seeded, simulated, reset, or restored by these tools.

## Architecture and migration

The original EDMX entities are reused: `AspNetUsers`, `AspNetRoles`, `Candidate`, `Structure`, `CandidateStructure`, and `VotingSession`. Each of the six positions has its own `VotingSession`, related by the additive `DemoContest` table. The presidential session fills two offices from one ballot. This avoids changing the generated EDMX or replacing the application's database design.

The old `userVotes` table couples usernames to candidate choices, so it is deliberately unused in the demo. `Database/Demo.sql` adds:

- `DemoInstallation`: this local instance's marker and authorized administrator.
- `DemoAccount`: synthetic account kind and eligibility.
- `DemoContest` / `DemoNomination`: lifecycle, seat allocation and approved runoff nominees.
- `DemoParticipation`: voter and contest only, with a unique key preventing repeat voting.
- `DemoBallot`: random GUID, contest and choice only. No voter, timestamp or receipt key.
- `DemoSimulation`: seed, distribution, total ballots and run time; no voter-choice mappings.

`BallotService` is the common entry point for the normal vote controller and simulated ballots. Its demo persistence path checks synthetic eligibility, nominations, lifecycle, voting dates and duplicate participation under a serialized transaction. Manual and simulated ballots use the same validation implementation. The non-demo compatibility adapter retains the original database workflow.

## Guards

All demo operations require `AppEnvironment=Development`, `Demo:Enabled=true`, a matching installation marker, matching Identity/EDMX connections, Windows-integrated LocalDB access, and the exact dedicated demo database name (or the strictly named disposable test database). Requests must come from the local computer. Every marked synthetic account must have the expected synthetic ID/name and an `example.test` email. The private local directory is restricted to the current developer, SYSTEM and local administrators. The dashboard requires `DEMO_ADMIN`, and services also verify the configured administrator's identity. All mutations require POST and anti-forgery tokens.

These flags are disabled in the source Web.config. They are generated only inside ignored `.demo-local/` deployment files. Demo cookies are separated from the normal site's cookies. If the existing administrator uses two-factor authentication, setup refuses to silently disable it; that provider must be configured for the isolated instance first. The demo blocks normal registration and ordinary administration edits so genuine student data cannot accidentally enter the simulation. Password/security changes through the demo UI are disabled.

No email/SMS is sent. Never publish `.demo-local/`, database backups, copied authentication material or generated local configs. `.gitignore` excludes them. Backups are sensitive and should stay on the development computer.

## Prepare and sign in

For a clean clone, first create your own local administrator using [the reviewer setup](../SETUP.md#create-a-local-administrator). Replace `YOUR-REGISTERED-STUDENT-ID` with your own account's student ID in the examples below; existing local accounts are not distributed.

Build first in Visual Studio or with the MSBuild command in [SETUP.md](../SETUP.md#restore-and-build). Then, from the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Demo\Initialize-Demo.ps1 -AdminStudentId YOUR-REGISTERED-STUDENT-ID
```

The script:

1. Reads only the existing authorized administrator's authentication ID/name/hash/stamp from the current database. It does not read/copy real student lists, email or phone data.
2. Creates `online_voting_system_demo` if necessary and takes an initial backup before schema changes.
3. Installs the baseline schema and additive demo tables.
4. Preserves the administrator's existing password; no password is requested, printed, reset or embedded in SQL/source files.
5. Deploys this same application to `.demo-local/site`, seeds through ASP.NET Identity, and starts a separate IIS Express instance on port **5099**.

Open **http://localhost:5099/Demo** and sign in with student ID **YOUR-REGISTERED-STUDENT-ID** and the password already registered for that account. This is an authentication-only copy for the isolated demo, not an eligible simulated voter. Sign in again if the real site's cookie was previously in use.

Use `-NoLaunch` to prepare without starting IIS Express. On redeployment, setup stops only the matching demo IIS Express process recorded in `.demo-local/iis.pid` before replacing its files. Repeating seed operations does not duplicate records or clear existing votes.

## Synthetic accounts and manifestos

There are exactly 19 fictional candidates across the requested six positions. Every candidate's `userAccountId` points to an actual Identity primary key (`demo-candidate-01` etc.), never an email address. There are 200 dedicated synthetic voter accounts (`demo.voter.001` through `demo.voter.200`). Candidate accounts (`demo.candidate.01` through `.19`) are also eligible, giving **219 eligible accounts**. The default simulator uses the 200 dedicated voters, so a full base round has **1,200 ballots** and **91.3% turnout per contest**.

All synthetic addresses use `example.test`. Identity accounts are initially provisioned without a password, so no shared demonstration credential exists. Candidate manifestos are neutral fictional student-service statements served by the demo view model; original manifesto upload paths and generated EDMX files are unchanged.

For a manual-voting demonstration, provision one synthetic account locally:

```powershell
powershell -ExecutionPolicy Bypass -File .\Demo\Set-DemoVoterPassword.ps1 -UserName demo.voter.001
```

Enter the new test account password only in that secure local prompt. It is hashed with ASP.NET Identity and is neither printed nor saved as plaintext. This command rejects the administrator and non-demo accounts. Sign out of the demo administrator session and sign in as that synthetic student to use `/DemoPublic/Ballot`. Use a candidate account similarly to demonstrate candidate eligibility. These credentials are never displayed in the production UI.

## Full election walkthrough

Every page/result is visibly labelled **DEMO / SIMULATED**.

1. **Create / verify demo data**: 19 candidates, 200 dedicated voters, 219 eligible, 6 base contests. This is idempotent.
2. **Schedule drafts**: defaults begin just before now and end tomorrow, in the server's local timezone. Future schedules cannot be opened early.
3. **Open scheduled contests**.
4. **Simulate validated votes**: choose `Preset`, random seed `2026`, and `200` voters. All 1,200 ballots pass the same voting rules as manual submission. Repeat submissions cannot create duplicates.
5. **Close open contests**.
6. Review persisted totals and verified participation counts. Results remain provisional until publication.
7. **Verify and publish DEMO results**. Open **Published DEMO results**.
8. **Reset demo simulation** for another demonstration. A backup is mandatory before reset. The administrator password, candidates, accounts and base nominations are preserved; ballots, runoffs, schedules and publication state are cleared.

Preset base totals:

| Contest | Votes in the listed candidate order | Result |
|---|---|---|
| President | 80 / 60 / 40 / 20 | Tatenda Moyo: President; Rumbidzai Ndlovu: Vice President |
| Secretary General | 100 / 60 / 40 | Farai Sibanda |
| Treasurer | 100 / 60 / 40 | Tafadzwa Ncube |
| Sports and Recreation Secretary | 100 / 60 / 40 | Kudzai Mupfumi |
| Academic Affairs Secretary | 100 / 60 / 40 | Rufaro Makoni |
| Student Welfare Secretary | 100 / 60 / 40 | Anesu Mapuranga |

These are explicit fictional fixture distributions, not genuine votes. Winners are always calculated by counting persisted anonymous ballots; candidate insertion order or database IDs never determine election outcomes. `Random` is reproducible for a fixed seed on a fresh reset. Do not interpret a known simulation seed as providing unpredictability: synthetic distributions are intentionally reproducible.

### Ties and runoffs

After reset, run the same lifecycle using `Tie`. All six base contests have an unresolved highest-place tie. Publication is blocked. Click **Approve runoff for tied candidates** on each closed tied contest. The administrator's action explicitly approves the new round; no alphabetical or ID tie-break occurs.

Schedule the new draft runoffs, open them, simulate using `Preset`, close them, and publish. Only tied candidates appear in each runoff. A presidential top-place tie fills President and Vice President from the same runoff. `ViceTie` produces 80 / 60 / 60 / 0 in the presidential contest: President is retained and only the tied Vice President candidates enter a one-seat runoff. Repeated runoff ties require another approved round.

## Ballot secrecy boundary

Participation and choices are separated at the application/database-schema level. There is no stored voter-to-ballot identifier or choice timestamp, and dashboards never expose individual participation linked to choices. SQL administrators with transaction-log/forensic access are outside this logical separation boundary. This is not a cryptographic, independently audited secret-ballot system and should not be represented as one. Known synthetic seeds/distributions are deliberately reproducible; use neither the simulation nor the legacy real-app ballot schema for consequential elections without an independent privacy/security design review.

## Backup, rollback and removal

Initial, seed and reset backups are saved under `.demo-local/backups`. They are excluded from source control. The dashboard also has a backup button.

To restore a selected backup (only this marked demo database can be targeted):

```powershell
powershell -ExecutionPolicy Bypass -File .\Demo\Restore-Demo.ps1 -BackupFile "C:\Projects\OnlineVotingSystem\.demo-local\backups\<selected-file>.bak"
```

The restore command checks the backup database name and current installation marker, takes a fresh pre-restore backup, stops only the demo IIS Express process, and restores only `online_voting_system_demo`. Relaunch with the initializer afterward. The real database is never a valid restore target. Restoring the initial empty backup requires running setup again.

To remove the demo entirely, stop the demo process and remove only the dedicated `online_voting_system_demo` database and `.demo-local` directory after retaining any required private backups. Do not run deletion commands against the normal application database.

## Tests

```powershell
powershell -ExecutionPolicy Bypass -File .\Verification\Test-Demo.ps1 -AdminStudentId YOUR-REGISTERED-STUDENT-ID
```

The tests create their own strictly named demo test database, back it up, copy only the administrator's authentication material, and use synthetic fixture data. Tests cover seeding/Identity foreign keys, candidate eligibility, concurrent duplicates, secret-ballot schema boundaries, timing/lifecycle, persisted tallies, random reproducibility, both presidency tie cases, approved runoffs, publication, password preservation, reset safety and fail-closed configuration. Test databases are removed; private backups remain under ignored `.demo-local` directories. See `Verification/demo-last-run.txt` for the latest results.




