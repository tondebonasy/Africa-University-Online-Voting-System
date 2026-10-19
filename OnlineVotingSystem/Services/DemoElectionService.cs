using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using OnlineVotingSystem.Models;

namespace OnlineVotingSystem.Services
{
    internal static class DemoDb
    {
        internal static SqlCommand Command(SqlConnection c, SqlTransaction t, string sql, params object[] args)
        {
            var cmd = new SqlCommand(sql, c, t) { CommandTimeout = 120 };
            for (int i = 0; i < args.Length; i += 2) cmd.Parameters.AddWithValue((string)args[i], args[i + 1] ?? DBNull.Value);
            return cmd;
        }
        internal static object Scalar(SqlConnection c, SqlTransaction t, string sql, params object[] args) { using (var cmd = Command(c,t,sql,args)) return cmd.ExecuteScalar(); }
        internal static int Number(SqlConnection c, SqlTransaction t, string sql, params object[] args) { return Convert.ToInt32(Scalar(c,t,sql,args)); }
        internal static void Execute(SqlConnection c, SqlTransaction t, string sql, params object[] args) { using (var cmd = Command(c,t,sql,args)) cmd.ExecuteNonQuery(); }
        internal static DataTable Rows(SqlConnection c, SqlTransaction t, string sql, params object[] args) { using (var cmd = Command(c,t,sql,args)) using (var adapter = new SqlDataAdapter(cmd)) { var table = new DataTable(); adapter.Fill(table); return table; } }
        internal static void Lock(SqlConnection c, SqlTransaction t)
        {
            if (Number(c,t,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'DemoElectionWrite',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000; SELECT @r;") < 0) throw new InvalidOperationException("Another demo operation is running. Please try again.");
        }
    }
    public sealed class DemoElectionService
    {
        public static readonly string[][] CandidateNames = {
            new[] { "Tatenda Moyo", "Rumbidzai Ndlovu", "Tinashe Dube", "Nyasha Chikowore" },
            new[] { "Farai Sibanda", "Chipo Mlambo", "Brian Chirwa" },
            new[] { "Tafadzwa Ncube", "Grace Mutasa", "Simbarashe Zhou" },
            new[] { "Kudzai Mupfumi", "Panashe Banda", "Munashe Hove" },
            new[] { "Rufaro Makoni", "Tanaka Mhere", "Shamiso Dzingai" },
            new[] { "Anesu Mapuranga", "Ropafadzo Gumbo", "Blessing Chuma" }
        };
        public static readonly string[] Positions = { "President", "Secretary General", "Treasurer", "Sports and Recreation Secretary", "Academic Affairs Secretary", "Student Welfare Secretary" };
        private static readonly string[] Codes = { "president", "secretary", "treasurer", "sports", "academic", "welfare" };
        private static readonly string[] Priorities = { "open student consultations, accessible leadership and clear reporting", "accurate meeting records, timely notices and a dependable student information calendar", "clear budgets, responsible spending and transparent student activity reports", "inclusive recreation, fair access to sports facilities and varied student activities", "academic feedback channels, study support and better communication on learning resources", "student wellbeing, accessible support information and inclusive campus services" };
        private static void Admin(SqlConnection c, SqlTransaction t, string actor)
        {
            if (DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.AspNetUsers u JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE u.Id=@id AND u.UserName=@name AND r.Name=N'DEMO_ADMIN'", "@id",DemoEnvironment.AdminId,"@name",actor) != 1) throw new InvalidOperationException("Only the explicitly authorized development demo administrator can perform this operation.");
        }
        public string Backup(string actor)
        {
            using (var c = DemoEnvironment.Open())
            {
                Admin(c,null,actor);
                var root = Path.GetFullPath(System.Configuration.ConfigurationManager.AppSettings["Demo:RootDirectory"] ?? "");
                var folder = Path.GetFullPath(DemoEnvironment.BackupDirectory ?? "");
                if (!Path.IsPathRooted(folder) || folder.StartsWith(@"\\") || !folder.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe demo backup location.");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, DemoEnvironment.DatabaseName + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".bak");
                DemoDb.Execute(c,null,"BACKUP DATABASE [" + DemoEnvironment.DatabaseName + "] TO DISK=@path WITH COPY_ONLY, INIT, CHECKSUM", "@path",path);
                return Path.GetFileName(path);
            }
        }
        private static void SafeInventory(SqlConnection c, SqlTransaction t)
        {
            if (DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoAccount a JOIN dbo.AspNetUsers u ON u.Id=a.UserId WHERE u.Id=@admin OR u.Email IS NULL OR u.Email NOT LIKE N'%@example.test' OR (a.Kind='Voter' AND (u.Id NOT LIKE N'demo-voter-[0-9][0-9][0-9]' OR u.UserName NOT LIKE N'demo.voter.[0-9][0-9][0-9]')) OR (a.Kind='Candidate' AND (u.Id NOT LIKE N'demo-candidate-[0-9][0-9]' OR u.UserName NOT LIKE N'demo.candidate.[0-9][0-9]'))", "@admin",DemoEnvironment.AdminId) != 0)
                throw new InvalidOperationException("A marked account is not a reserved-domain synthetic identity. Demo operations are blocked.");
            if (DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.AspNetUsers u WHERE u.Id<>@admin AND NOT EXISTS(SELECT 1 FROM dbo.DemoAccount a WHERE a.UserId=u.Id)","@admin",DemoEnvironment.AdminId) != 0 ||
                DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.Candidate c WHERE NOT EXISTS(SELECT 1 FROM dbo.DemoAccount a WHERE a.UserId=c.userAccountId AND a.Kind='Candidate')") != 0 ||
                DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.VotingSession s WHERE NOT EXISTS(SELECT 1 FROM dbo.DemoContest d WHERE d.SessionId=s.Id)") != 0 ||
                DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.Structure s WHERE NOT EXISTS(SELECT 1 FROM dbo.DemoContest d WHERE d.StructureId=s.id)") != 0 ||
                DemoDb.Number(c,t,"SELECT (SELECT COUNT(*) FROM dbo.userVotes)+(SELECT COUNT(*) FROM dbo.Votes)") != 0)
                throw new InvalidOperationException("Unexpected non-demo data detected. Refusing to seed, simulate or reset this database.");
        }
        public async Task SeedAsync(string actor)
        {
            using (var c = DemoEnvironment.Open()) { Admin(c,null,actor); SafeInventory(c,null); if (DemoDb.Number(c,null,"SELECT CAST(Seeded AS int) FROM dbo.DemoInstallation WHERE Id=1") == 1) return; }
            Backup(actor);
            Database.SetInitializer<ApplicationDbContext>(null);
            using (var identity = new ApplicationDbContext())
            using (var tx = identity.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var c = (SqlConnection)identity.Database.Connection; var t = (SqlTransaction)tx.UnderlyingTransaction;
                DemoDb.Lock(c,t); Admin(c,t,actor); SafeInventory(c,t);
                if (DemoDb.Number(c,t,"SELECT CAST(Seeded AS int) FROM dbo.DemoInstallation WHERE Id=1") == 1) return;
                using (var users = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identity) { DisposeContext = false }))
                {
                    users.UserValidator = new UserValidator<ApplicationUser>(users) { AllowOnlyAlphanumericUserNames = false, RequireUniqueEmail = true };
                    int candidateIndex = 0;
                    for (var position = 0; position < Positions.Length; position++)
                    {
                        int structure = DemoDb.Number(c,t,"INSERT dbo.Structure(name,rank) VALUES(@name,@rank); SELECT CAST(SCOPE_IDENTITY() AS int)","@name",Positions[position],"@rank",position+1);
                        int session = DemoDb.Number(c,t,"INSERT dbo.VotingSession(createdBy,status) VALUES(N'DEMO',N'Draft'); SELECT CAST(SCOPE_IDENTITY() AS int)");
                        DemoDb.Execute(c,t,"INSERT dbo.DemoContest(SessionId,StructureId,Code,Phase,Seats,SeatLabel) VALUES(@session,@structure,@code,'Draft',@seats,@label)","@session",session,"@structure",structure,"@code",Codes[position],"@seats",position==0?2:1,"@label",Positions[position]);
                        foreach (var name in CandidateNames[position])
                        {
                            candidateIndex++;
                            var user = new ApplicationUser { Id = "demo-candidate-" + candidateIndex.ToString("D2"), UserName = "demo.candidate." + candidateIndex.ToString("D2"), Email = "candidate" + candidateIndex.ToString("D2") + "@example.test", LockoutEnabled = true };
                            var result = await users.CreateAsync(user).ConfigureAwait(false);
                            if (!result.Succeeded) throw new InvalidOperationException("Demo Identity provisioning failed: " + String.Join("; ",result.Errors));
                            DemoDb.Execute(c,t,"INSERT dbo.DemoAccount(UserId,Kind,Eligible) VALUES(@user,'Candidate',1)","@user",user.Id);
                            int candidate = DemoDb.Number(c,t,"INSERT dbo.Candidate(name,userAccountId) VALUES(@name,@user); SELECT CAST(SCOPE_IDENTITY() AS int)","@name",name,"@user",user.Id);
                            int nomination = DemoDb.Number(c,t,"INSERT dbo.CandidateStructure(candidateId,structureId) VALUES(@candidate,@structure); SELECT CAST(SCOPE_IDENTITY() AS int)","@candidate",candidate,"@structure",structure);
                            DemoDb.Execute(c,t,"INSERT dbo.DemoNomination(SessionId,NominationId) VALUES(@session,@nomination)","@session",session,"@nomination",nomination);
                        }
                    }
                    for(int i=1;i<=200;i++)
                    {
                        var user = new ApplicationUser { Id = "demo-voter-" + i.ToString("D3"), UserName = "demo.voter." + i.ToString("D3"), Email = "voter" + i.ToString("D3") + "@example.test", LockoutEnabled = true };
                        var result = await users.CreateAsync(user).ConfigureAwait(false);
                        if (!result.Succeeded) throw new InvalidOperationException("Synthetic voter provisioning failed.");
                        DemoDb.Execute(c,t,"INSERT dbo.DemoAccount(UserId,Kind,Eligible) VALUES(@user,'Voter',1)","@user",user.Id);
                    }
                }
                DemoDb.Execute(c,t,"UPDATE dbo.DemoInstallation SET Seeded=1 WHERE Id=1");
                tx.Commit();
            }
        }
        public void Schedule(string actor, DateTime start, DateTime end)
        {
            if (start < new DateTime(1753,1,1) || end<=start || end<=DateTime.Now) throw new InvalidOperationException("Choose an end time after the start time and after now. Times use the server's local timezone.");
            Mutate(actor,(c,t)=>{
                if (DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoContest WHERE Phase='Draft'")==0) throw new InvalidOperationException("There are no draft contests to schedule.");
                DemoDb.Execute(c,t,"UPDATE s SET startDay=@start,endDate=@end,status=N'Scheduled' FROM dbo.VotingSession s JOIN dbo.DemoContest d ON d.SessionId=s.Id WHERE d.Phase='Draft'; UPDATE dbo.DemoContest SET Phase='Scheduled' WHERE Phase='Draft'", "@start",start,"@end",end);
            });
        }
        public void Open(string actor)
        {
            Mutate(actor,(c,t)=>{
                if (DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoContest WHERE Phase='Scheduled'")==0) throw new InvalidOperationException("Schedule the draft contests first.");
                if (DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoContest d JOIN dbo.VotingSession s ON s.Id=d.SessionId WHERE d.Phase='Scheduled' AND (s.startDay>GETDATE() OR s.endDate<=GETDATE() OR s.startDay IS NULL OR s.endDate IS NULL)")>0) throw new InvalidOperationException("Scheduled contests are outside their voting period. Opening early or after the deadline is forbidden.");
                DemoDb.Execute(c,t,"UPDATE s SET status=N'Open' FROM dbo.VotingSession s JOIN dbo.DemoContest d ON d.SessionId=s.Id WHERE d.Phase='Scheduled'; UPDATE dbo.DemoContest SET Phase='Open' WHERE Phase='Scheduled'");
            });
        }
        public void Close(string actor)
        {
            Mutate(actor,(c,t)=>{
                if (DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoContest WHERE Phase='Open'")==0) throw new InvalidOperationException("There are no open contests.");
                DemoDb.Execute(c,t,"UPDATE s SET endDate=CASE WHEN endDate>GETDATE() THEN GETDATE() ELSE endDate END,status=N'Closed' FROM dbo.VotingSession s JOIN dbo.DemoContest d ON d.SessionId=s.Id WHERE d.Phase='Open'; UPDATE dbo.DemoContest SET Phase='Closed' WHERE Phase='Open'");
            });
        }
        private static void Mutate(string actor, Action<SqlConnection,SqlTransaction> action)
        {
            using(var c=DemoEnvironment.Open()) using(var t=c.BeginTransaction(IsolationLevel.Serializable))
            { DemoDb.Lock(c,t); Admin(c,t,actor); SafeInventory(c,t); action(c,t); t.Commit(); }
        }
        internal static BallotOutcome CastInside(SqlConnection c, SqlTransaction t, string username, int nomination, int? explicitSession=null)
        {
            var user = DemoDb.Rows(c,t,"SELECT u.Id FROM dbo.AspNetUsers u JOIN dbo.DemoAccount a ON a.UserId=u.Id WHERE u.UserName=@name AND a.Eligible=1", "@name",username);
            if(user.Rows.Count!=1) return new BallotOutcome { Message="DEMO: This account is not an eligible synthetic voter." };
            var sessions=DemoDb.Rows(c,t,"SELECT d.SessionId FROM dbo.DemoContest d JOIN dbo.DemoNomination n ON n.SessionId=d.SessionId JOIN dbo.VotingSession s ON s.Id=d.SessionId WHERE n.NominationId=@nomination AND d.Phase='Open' AND s.startDay<=GETDATE() AND s.endDate>GETDATE() AND (@session IS NULL OR d.SessionId=@session)","@nomination",nomination,"@session",(object)explicitSession??DBNull.Value);
            if(sessions.Rows.Count!=1) return new BallotOutcome { Message="DEMO: This contest is not open within its scheduled voting period." };
            var session=(int)sessions.Rows[0]["SessionId"]; var userId=(string)user.Rows[0]["Id"];
            if(DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoParticipation WHERE SessionId=@session AND UserId=@user", "@session",session,"@user",userId)>0) return new BallotOutcome { Message="DEMO: You have already voted in this contest." };
            DemoDb.Execute(c,t,"INSERT dbo.DemoParticipation(SessionId,UserId) VALUES(@session,@user)","@session",session,"@user",userId);
            // A fresh cryptographic random ballot ID; no timestamp or receipt link is stored.
            DemoDb.Execute(c,t,"INSERT dbo.DemoBallot(BallotId,SessionId,NominationId) VALUES(@ballot,@session,@nomination)","@ballot",Guid.NewGuid(),"@session",session,"@nomination",nomination);
            return new BallotOutcome { Success=true, Message="DEMO / SIMULATED: Your demonstration vote has been recorded." };
        }
        public BallotOutcome Cast(string username,int nomination)
        {
            using(var c=DemoEnvironment.Open()) using(var t=c.BeginTransaction(IsolationLevel.Serializable))
            { DemoDb.Lock(c,t); SafeInventory(c,t); var result=CastInside(c,t,username,nomination); if(result.Success)t.Commit(); return result; }
        }
        public int Simulate(string actor,int seed,string mode,int voters)
        {
            if(voters<1||voters>200) throw new InvalidOperationException("Select between 1 and 200 dedicated synthetic voters.");
            if(mode!="Preset"&&mode!="Random"&&mode!="Tie"&&mode!="ViceTie") throw new InvalidOperationException("Unknown simulation mode.");
            int ballots=0;
            Mutate(actor,(c,t)=>{
                var contests=DemoDb.Rows(c,t,"SELECT SessionId FROM dbo.DemoContest WHERE Phase='Open' ORDER BY SessionId");
                if(contests.Rows.Count==0) throw new InvalidOperationException("Open a scheduled demo contest before simulating.");
                var random=new Random(seed);
                foreach(DataRow contest in contests.Rows)
                {
                    int session=(int)contest["SessionId"];
                    var people=DemoDb.Rows(c,t,"SELECT u.UserName FROM dbo.AspNetUsers u JOIN dbo.DemoAccount a ON a.UserId=u.Id WHERE a.Kind='Voter' AND a.Eligible=1 AND NOT EXISTS(SELECT 1 FROM dbo.DemoParticipation p WHERE p.SessionId=@session AND p.UserId=u.Id) ORDER BY u.Id", "@session",session).Rows.Cast<DataRow>().Take(voters).Select(r=>(string)r[0]).ToList();
                    // Restrict choices to this specific base contest or approved runoff.
                    var nominees=DemoDb.Rows(c,t,"SELECT n.NominationId FROM dbo.DemoNomination n JOIN dbo.CandidateStructure cs ON cs.id=n.NominationId JOIN dbo.Candidate ca ON ca.id=cs.candidateId WHERE n.SessionId=@session ORDER BY ca.userAccountId", "@session",session);
                    int count=nominees.Rows.Count;
                    if(count<2)throw new InvalidOperationException("A contest needs at least two valid nominees.");
                    var votes=new List<int>();
                    if(mode=="Random") for(int i=0;i<people.Count;i++) votes.Add(random.Next(count));
                    else
                    {
                        var weights=count==4?(mode=="Tie"?new[]{35,35,15,15}:mode=="ViceTie"?new[]{40,30,30,0}:new[]{40,30,20,10}):count==3?(mode=="Tie"?new[]{40,40,20}:new[]{50,30,20}):new[]{60,40};
                        int assigned=0;
                        for(int i=0;i<count;i++) { int quota=i==count-1?people.Count-assigned:people.Count*weights[i]/100; assigned+=quota; for(int j=0;j<quota;j++)votes.Add(i); }
                    }
                    // Shuffle the synthetic voter order; never persist a voter-choice map.
                    for(int i=people.Count-1;i>0;i--){int j=random.Next(i+1);var value=people[i];people[i]=people[j];people[j]=value;}
                    for(int i=0;i<people.Count;i++) { var result=BallotService.CastDemoValidated(c,t,people[i],(int)nominees.Rows[votes[i]][0],session); if(!result.Success)throw new InvalidOperationException(result.Message); ballots++; }
                }
                DemoDb.Execute(c,t,"INSERT dbo.DemoSimulation(RandomSeed,Mode,Ballots) VALUES(@seed,@mode,@ballots)","@seed",seed,"@mode",mode,"@ballots",ballots);
            });
            return ballots;
        }
        public DemoDashboard Snapshot(string actor=null, bool publishedOnly=false)
        {
            using(var c=DemoEnvironment.Open()) using(var t=c.BeginTransaction(IsolationLevel.Serializable))
            {
                if(!publishedOnly)Admin(c,t,actor); SafeInventory(c,t);
                var result=ReadDashboard(c,t);
                if(publishedOnly && !result.Published) { result.Contests.Clear(); result.Ballots=0; }
                t.Commit(); return result;
            }
        }
        public DemoDashboard BallotSnapshot()
        {
            using(var c=DemoEnvironment.Open()) using(var t=c.BeginTransaction(IsolationLevel.Serializable))
            {
                SafeInventory(c,t); var result=ReadDashboard(c,t);
                foreach(var contest in result.Contests) { contest.Choices.ForEach(choice=>choice.Votes=0); contest.Allocation=null; contest.Participation=0; }
                result.Ballots=0; t.Commit(); return result;
            }
        }
        private static DemoDashboard ReadDashboard(SqlConnection c,SqlTransaction t)
        {
            var result=new DemoDashboard { Seeded=DemoDb.Number(c,t,"SELECT CAST(Seeded AS int) FROM dbo.DemoInstallation WHERE Id=1")==1, Candidates=DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoAccount WHERE Kind='Candidate'"), Voters=DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoAccount WHERE Kind='Voter'"), Eligible=DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoAccount WHERE Eligible=1"), Ballots=DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoBallot") };
            foreach(DataRow row in DemoDb.Rows(c,t,"SELECT d.*,s.name AS Position,ses.startDay,ses.endDate FROM dbo.DemoContest d JOIN dbo.Structure s ON s.id=d.StructureId JOIN dbo.VotingSession ses ON ses.Id=d.SessionId ORDER BY d.SessionId").Rows)
            {
                var contest=new DemoContestView { SessionId=(int)row["SessionId"],Name=(string)row["Position"],Phase=(string)row["Phase"],Seats=(int)row["Seats"],SeatLabel=(string)row["SeatLabel"],ParentSessionId=row.IsNull("ParentSessionId")?(int?)null:(int)row["ParentSessionId"],Start=row.IsNull("startDay")?(DateTime?)null:(DateTime)row["startDay"],End=row.IsNull("endDate")?(DateTime?)null:(DateTime)row["endDate"] };
                contest.Participation=DemoDb.Number(c,t,"SELECT COUNT(*) FROM dbo.DemoParticipation WHERE SessionId=@session","@session",contest.SessionId);
                foreach(DataRow choice in DemoDb.Rows(c,t,"SELECT n.NominationId,ca.name,ca.userAccountId,COUNT(b.BallotId) AS Votes FROM dbo.DemoNomination n JOIN dbo.CandidateStructure cs ON cs.id=n.NominationId JOIN dbo.Candidate ca ON ca.id=cs.candidateId LEFT JOIN dbo.DemoBallot b ON b.SessionId=n.SessionId AND b.NominationId=n.NominationId WHERE n.SessionId=@session GROUP BY n.NominationId,ca.name,ca.userAccountId ORDER BY ca.userAccountId","@session",contest.SessionId).Rows)
                {
                    int index; var key=(string)choice["userAccountId"];int.TryParse(key.Substring(key.LastIndexOf('-')+1),out index);
                    var priority=Array.FindIndex(Positions,p=>p==contest.Name);
                    contest.Choices.Add(new DemoChoice { NominationId=(int)choice["NominationId"],Name=(string)choice["name"],Votes=(int)choice["Votes"],Manifesto="Fictional demonstration manifesto: I would focus on " + Priorities[Math.Max(0,priority)] + ". I would gather student feedback, publish clear progress updates and work with relevant university teams. This is synthetic campaign content, not a real candidate's statement." });
                }
                if(contest.Choices.Sum(v=>v.Votes)!=contest.Participation)throw new InvalidOperationException("Ballot integrity verification failed: participation and anonymous ballot totals do not match.");
                contest.Allocation=DemoRules.Allocate(contest.Choices,contest.Seats,contest.SeatLabel);
                result.Contests.Add(contest);
            }
            foreach(var contest in result.Contests.Where(v=>!v.ParentSessionId.HasValue)) ResolveRunoffs(contest,result.Contests,new HashSet<int>());
            var bases=result.Contests.Where(v=>!v.ParentSessionId.HasValue).ToList();
            result.Published=bases.Count==6&&bases.All(v=>v.Phase=="Published"&&v.Allocation.Resolved);
            return result;
        }
        private static void ResolveRunoffs(DemoContestView contest,List<DemoContestView> all,HashSet<int> visited)
        {
            if(!visited.Add(contest.SessionId))throw new InvalidOperationException("Invalid runoff chain.");
            var child=all.SingleOrDefault(v=>v.ParentSessionId==contest.SessionId);
            if(child!=null && (child.Phase=="Closed"||child.Phase=="Published"))
            {
                ResolveRunoffs(child,all,visited);
                if(child.Allocation.Resolved){contest.Allocation.Winners.AddRange(child.Allocation.Winners);contest.Allocation.TiedNominees.Clear();contest.Allocation.PendingSeats=0;}
            }
        }
        public void CreateRunoff(string actor,int session)
        {
            Mutate(actor,(c,t)=>{
                var snapshot=ReadDashboard(c,t);var parent=snapshot.Contests.SingleOrDefault(v=>v.SessionId==session);
                if(parent==null||parent.Phase!="Closed"||parent.Allocation.TiedNominees.Count==0||snapshot.Contests.Any(v=>v.ParentSessionId==session))throw new InvalidOperationException("Select a closed tied contest without an existing runoff. This action is administrator approval of a runoff, not an arbitrary tie-break.");
                int id=DemoDb.Number(c,t,"INSERT dbo.VotingSession(createdBy,status) VALUES(N'DEMO RUNOFF',N'Draft'); SELECT CAST(SCOPE_IDENTITY() AS int)");
                DemoDb.Execute(c,t,"INSERT dbo.DemoContest(SessionId,StructureId,Code,Phase,Seats,SeatLabel,ParentSessionId) SELECT @id,StructureId,Code+'-r','Draft',@seats,@label,SessionId FROM dbo.DemoContest WHERE SessionId=@parent", "@id",id,"@seats",parent.Allocation.PendingSeats,"@label",parent.Allocation.PendingSeat,"@parent",session);
                foreach(int nominee in parent.Allocation.TiedNominees)DemoDb.Execute(c,t,"INSERT dbo.DemoNomination(SessionId,NominationId) VALUES(@session,@nomination)","@session",id,"@nomination",nominee);
            });
        }
        public void Publish(string actor)
        {
            Mutate(actor,(c,t)=>{
                var snapshot=ReadDashboard(c,t);
                if(snapshot.Contests.Count(v=>!v.ParentSessionId.HasValue)!=6||snapshot.Contests.Any(v=>v.Phase!="Closed"&&v.Phase!="Published")||snapshot.Contests.Where(v=>!v.ParentSessionId.HasValue).Any(v=>!v.Allocation.Resolved))throw new InvalidOperationException("Close all contests and resolve every tie by approved runoff before publication. Zero-vote contests cannot be published.");
                DemoDb.Execute(c,t,"UPDATE dbo.DemoContest SET Phase='Published'; UPDATE s SET status=N'Published' FROM dbo.VotingSession s JOIN dbo.DemoContest d ON d.SessionId=s.Id");
            });
        }
        public void Reset(string actor)
        {
            Backup(actor);
            Mutate(actor,(c,t)=>{
                DemoDb.Execute(c,t,"DELETE dbo.DemoBallot; DELETE dbo.DemoParticipation; DELETE dbo.DemoSimulation; DELETE n FROM dbo.DemoNomination n JOIN dbo.DemoContest d ON d.SessionId=n.SessionId WHERE d.ParentSessionId IS NOT NULL; WHILE EXISTS(SELECT 1 FROM dbo.DemoContest WHERE ParentSessionId IS NOT NULL) BEGIN DECLARE @leaf int; SELECT TOP 1 @leaf=d.SessionId FROM dbo.DemoContest d WHERE d.ParentSessionId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.DemoContest child WHERE child.ParentSessionId=d.SessionId); DELETE dbo.DemoContest WHERE SessionId=@leaf; DELETE dbo.VotingSession WHERE Id=@leaf; END; UPDATE dbo.DemoContest SET Phase='Draft'; UPDATE s SET startDay=NULL,endDate=NULL,status=N'Draft' FROM dbo.VotingSession s JOIN dbo.DemoContest d ON d.SessionId=s.Id;");
            });
        }
    }
}



