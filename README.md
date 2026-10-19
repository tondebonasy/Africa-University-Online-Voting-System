# Africa University Online Voting System

A student election portal built with ASP.NET MVC. It brings candidate information, scheduled voting and election administration into one responsive application.

## Features

- Student registration and role-based administration.
- Candidate profiles, manifesto previews and council positions.
- Scheduled ballots, duplicate-vote checks and election results.
- Responsive Bootstrap interface with locally hosted typography.
- Isolated election demonstration with synthetic accounts, repeatable simulations, approved runoffs and controlled results publication.

## Technology

C# · ASP.NET MVC 5 · .NET Framework 4.7.2 · Razor · Entity Framework 6 · SQL Server LocalDB · ASP.NET Identity / OWIN · Bootstrap 5.3.8 · jQuery

## Try the demonstration

Follow [SETUP.md](SETUP.md) to run the project and create your own local administrator. The [complete demonstration guide](Demo/README.md) covers 19 fictional candidates, six contests, 200 synthetic voters and the full election lifecycle. No shared login credentials or existing student accounts are distributed.

**Ballot privacy differs by mode:** the isolated demo stores participation separately from choices. The legacy application associates voter usernames with candidate selections. The demo is not a cryptographic secret-ballot system; see [architecture and limitations](ARCHITECTURE.md).

## Testing

The verification suites cover registration, authentication, voting, administration, election lifecycle, ties and isolation guards. Latest publication checks: 39 regression checks, 39 demo checks and four configuration guards. Commands and scope are documented in [ARCHITECTURE.md](ARCHITECTURE.md#verification).

## Developer information

**Tonderai Bonasi** — Software Engineer | Full-Stack Developer

[GitHub](https://github.com/tondebonasy) · [Email](mailto:tonderaibonasy@gmail.com)

## Documentation and rights

- [Installation and local administrator setup](SETUP.md)
- [Architecture, security and verification](ARCHITECTURE.md)
- [Complete demonstration guide](Demo/README.md)
- [GitHub publication and security](GITHUB.md)
- [Asset permissions and third-party notices](ASSETS.md)

Public images are original neutral illustrations under a scoped MIT licence; unlicensed university originals are preserved privately and excluded. Existing copyright and third-party licence notices are retained. This portfolio project is not a claim of university endorsement or production election certification.

