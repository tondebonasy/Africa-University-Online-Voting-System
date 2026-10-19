# Publication review

Prepared on 19 October 2026. No commit or push was made during preparation. Election logic, controllers and database schema are unchanged. Public image URLs, project content entries, the shared header emblem/favicon and the static-asset checks were updated to use neutral replacements.

## Candidate file set

[PUBLICATION-FILES.txt](PUBLICATION-FILES.txt) lists every candidate path, including this report. The set consists of:

- `README.md`: concise portfolio introduction, stack, features and developer contact.
- `SETUP.md`: prerequisites, dependency restoration, initialization and local administrator setup.
- `ARCHITECTURE.md`: design, ballot privacy differences, limitations, security and test commands.
- `Demo/README.md`: complete preserved demonstration walkthrough, with generic administrator placeholders.
- `GITHUB.md`: Git publication and sensitive-file precautions.
- `ASSETS.md`: public image licensing, privately preserved originals and retained notices.
- `.gitignore`, the solution and application project/source, vendored frontend files and notices.
- `Database/`: clean initialization, repair and isolated-demo schema scripts.
- `Demo/`: local-only provisioning, backup/restore and synthetic password tools.
- `Verification/`: source tests only; local results and scratch deployments are excluded.

The public image candidates are three original neutral SVG illustrations with their scoped MIT licence. Unlicensed university logos, photographs and old favicons were preserved privately, hash verified and removed from the staged set. No blanket licence has been added to inherited source.

## Sensitive-data review

Git's actual ignore rules exclude the historical root SQL dump, uploaded manifestos, the uploaded student Access database, local demo deployments and authentication material, database files/backups, restored packages, binaries, machine-specific project settings, design screenshots and local test reports. These working files have not been deleted.

The candidate text files and configuration were inspected for hardcoded credentials, account records, email/phone data, tokens and private keys. No real student/account datasets or production credentials were identified in the candidate set. Synthetic candidate fixtures and `example.test` accounts are intentional. The developer's public contact email is included with explicit permission. The clean baseline seeds only ADMIN and SUPER_ADMIN role definitions; it contains no users, candidate records or ballots. User model fields such as PasswordHash are schema, not stored account data.

Source Web.config contains Windows-integrated LocalDB connections, not passwords; demo mode is disabled. Existing third-party copyright/licence headers and the Manrope OFL are retained. The original images were reviewed and replaced; their private manifest records preservation hashes. Public SVGs contain no external image references or university logos.

## Verification outcome

- Working-tree build including Razor view compilation: passed.
- Source-only snapshot without local packages, binaries, user settings or uploads: NuGet restoration passed; compilation including Razor views passed.
- Clean baseline initialization and repeated initialization: passed in an isolated temporary LocalDB instance; zero account/election records and two built-in roles. Temporary database/instance removed.
- Existing normal regression suite: 39 checks (including all three public image endpoints) passed in PowerShell 7.
- Existing demo suite: 39 checks and four configuration guards passed.
- Documentation: all 32 local file links/heading anchors resolve to publication candidates; the developer GitHub URL returned HTTP 200. The mailto address is syntactically valid; no email was sent.

The isolated initializer test precreated an empty database with unique temporary file paths because LocalDB instances on this computer share a default data directory containing the existing database's filenames. The exact initializer then installed and rechecked the schema. Normal regression tests also create a new uniquely named database from the baseline SQL. The existing normal and published demonstration databases were not reset.

## Outstanding findings

NuGet restore reports existing security advisories, including high-severity findings, including dependencies in the jQuery/jQuery Validation, ASP.NET Identity/OWIN and Newtonsoft.Json stacks. Restore succeeds, but passing tests do not resolve these advisories. Dependencies were left unchanged to preserve application behavior; evaluate and upgrade them separately before deployment. This is suitable as transparently documented portfolio source, not a security-certified production election release.

The normal smoke harness requires PowerShell 7; Windows PowerShell 5.1 runs do not satisfy its HTTP response assertions. Setup documentation now states the actual requirement. These checks do not constitute exhaustive penetration, browser, accessibility or load testing.

Original image permissions remain unconfirmed, so those originals are excluded. Public replacements are covered by the scoped licence in [ASSETS.md](ASSETS.md). Follow [GITHUB.md](GITHUB.md) to inspect the staged set and perform the first commit manually. No remote has been configured or pushed.


## Public image verification

The shared layout and background sizing, overlays, responsive rules and dimensions are retained. The public project has no photo downloads or image CDN dependency. Local browser access is disabled in Work settings, so a fresh visual browser check of rendered pages could not be performed. Verification uses SVG artwork inspection, XML/reference checks, Razor compilation, HTTP page/asset tests and the existing election suites; it does not claim visual validation of every screen.

Image refresh checks passed: all ten original files are preserved with matching SHA-256 checksums; every one of the 179 staged blobs was compared against the restricted originals with zero matches. The staged image set contains only the three licensed public SVGs and the existing licensed Glyphicons font. Public account/help pages and all SVG HTTP/MIME responses passed; 39 normal regression checks and 39 demo checks plus four guards passed. The existing demonstration remains published with 1,200 synthetic ballots. No commit or push was made.
