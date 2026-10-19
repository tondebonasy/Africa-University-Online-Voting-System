-- Additive demo-only schema. Apply only through Demo/Initialize-Demo.ps1.
IF DB_NAME() NOT LIKE N'online[_]voting[_]system[_]demo%'
    THROW 51000, 'Refusing to install demo tables outside the isolated demo database.', 1;
GO
IF OBJECT_ID('dbo.DemoInstallation') IS NULL
CREATE TABLE dbo.DemoInstallation(Id int NOT NULL PRIMARY KEY CHECK(Id=1), InstanceId uniqueidentifier NOT NULL, AdminUserId nvarchar(128) NOT NULL REFERENCES dbo.AspNetUsers(Id), Seeded bit NOT NULL DEFAULT 0);
GO
IF OBJECT_ID('dbo.DemoAccount') IS NULL
CREATE TABLE dbo.DemoAccount(UserId nvarchar(128) NOT NULL PRIMARY KEY REFERENCES dbo.AspNetUsers(Id), Kind varchar(12) NOT NULL CHECK(Kind IN ('Candidate','Voter')), Eligible bit NOT NULL DEFAULT 1);
GO
IF OBJECT_ID('dbo.DemoContest') IS NULL
CREATE TABLE dbo.DemoContest(SessionId int NOT NULL PRIMARY KEY REFERENCES dbo.VotingSession(Id), StructureId int NOT NULL REFERENCES dbo.Structure(id), Code varchar(80) NOT NULL UNIQUE, Phase varchar(12) NOT NULL CHECK(Phase IN ('Draft','Scheduled','Open','Closed','Published')), Seats int NOT NULL CHECK(Seats IN (1,2)), SeatLabel nvarchar(100) NOT NULL, ParentSessionId int NULL REFERENCES dbo.DemoContest(SessionId), CONSTRAINT CK_DemoContest_Parent CHECK(ParentSessionId IS NULL OR ParentSessionId<>SessionId));
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_DemoContest_Parent')
CREATE UNIQUE INDEX UX_DemoContest_Parent ON dbo.DemoContest(ParentSessionId) WHERE ParentSessionId IS NOT NULL;
GO
IF OBJECT_ID('dbo.DemoNomination') IS NULL
CREATE TABLE dbo.DemoNomination(SessionId int NOT NULL REFERENCES dbo.DemoContest(SessionId), NominationId int NOT NULL REFERENCES dbo.CandidateStructure(id), PRIMARY KEY(SessionId,NominationId));
GO
-- Participation contains no nomination/candidate, timestamp or ballot identifier.
IF OBJECT_ID('dbo.DemoParticipation') IS NULL
CREATE TABLE dbo.DemoParticipation(SessionId int NOT NULL REFERENCES dbo.DemoContest(SessionId), UserId nvarchar(128) NOT NULL REFERENCES dbo.DemoAccount(UserId), PRIMARY KEY(SessionId,UserId));
GO
-- Anonymous choices contain no voter, timestamp, participation key or sequence ID.
IF OBJECT_ID('dbo.DemoBallot') IS NULL
CREATE TABLE dbo.DemoBallot(BallotId uniqueidentifier NOT NULL DEFAULT NEWID() PRIMARY KEY, SessionId int NOT NULL, NominationId int NOT NULL, FOREIGN KEY(SessionId,NominationId) REFERENCES dbo.DemoNomination(SessionId,NominationId));
GO
IF OBJECT_ID('dbo.DemoSimulation') IS NULL
CREATE TABLE dbo.DemoSimulation(Id uniqueidentifier NOT NULL DEFAULT NEWID() PRIMARY KEY, RandomSeed int NOT NULL, Mode varchar(12) NOT NULL, Ballots int NOT NULL, RunAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
