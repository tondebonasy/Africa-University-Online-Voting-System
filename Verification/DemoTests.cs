using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using OnlineVotingSystem.Controllers;
using OnlineVotingSystem.Services;
class DemoTests
{
 static int count;
 static string actor=ConfigurationManager.AppSettings["Demo:AdminUserName"];
 static DemoElectionService service=new DemoElectionService();
 static void Assert(bool condition,string name){if(!condition)throw new Exception("FAILED: "+name);count++;Console.WriteLine("PASS: "+name);}
 static void Reject(Action action,string name){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Assert(rejected,name);}
 static object Sql(string text){using(var c=DemoEnvironment.Open())using(var cmd=new SqlCommand(text,c)){cmd.CommandTimeout=120;return cmd.ExecuteScalar();}}
 static DemoChoice[] Choices(params int[] votes){return votes.Select((v,i)=>new DemoChoice{NominationId=100-i,Name="Fictional "+i,Votes=v}).ToArray();}
 static void Start(string mode){service.Schedule(actor,DateTime.Now.AddMinutes(-1),DateTime.Now.AddHours(1));service.Open(actor);service.Simulate(actor,2026,mode,200);service.Close(actor);}
 static int Main()
 {
  try{
   var allocation=DemoRules.Allocate(Choices(40,80,20,60),2,"President");Assert(allocation.Winners[0].NominationId==99&&allocation.Winners[1].NominationId==97,"Unit: President/VP allocation follows counts, not IDs or insertion order");
   allocation=DemoRules.Allocate(Choices(70,70,30,30),2,"President");Assert(allocation.Winners.Count==0&&allocation.PendingSeats==2,"Unit: top presidential tie blocks both seats");
   allocation=DemoRules.Allocate(Choices(80,60,60,0),2,"President");Assert(allocation.Winners.Single().Seat=="President"&&allocation.PendingSeat=="Vice President","Unit: second-place tie preserves President and requires Vice President runoff");
   Assert(!DemoRules.Allocate(Choices(0,0,0),1,"Treasurer").Resolved,"Unit: zero votes cannot produce a winner");
   Assert(DemoRules.Allocate(Choices(80,80,40),1,"Treasurer").TiedNominees.Count==2,"Unit: single-seat ties are explicit");
   var actions=typeof(DemoController).GetMethods().Where(m=>Attribute.IsDefined(m,typeof(HttpPostAttribute))).ToList();Assert(actions.Count==9&&actions.All(m=>Attribute.IsDefined(m,typeof(ValidateAntiForgeryTokenAttribute))),"Unit: every demo mutation requires POST and CSRF validation");
   Assert(((AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(DemoController),typeof(AuthorizeAttribute))).Roles=="DEMO_ADMIN","Unit: dashboard requires the explicit development administrator role");
   Reject(()=>service.Reset("not-authorized"),"Integration: unauthorized administrator cannot reset");
   service.SeedAsync(actor).GetAwaiter().GetResult();service.SeedAsync(actor).GetAwaiter().GetResult();var snapshot=service.Snapshot(actor);
   Assert(snapshot.Candidates==19&&snapshot.Voters==200&&snapshot.Eligible==219&&snapshot.Contests.Count==6,"Integration: idempotent Identity seeding creates exact requested inventory");
   Assert(Convert.ToInt32(Sql("SELECT COUNT(*) FROM dbo.Candidate c JOIN dbo.AspNetUsers u ON u.Id=c.userAccountId JOIN dbo.DemoAccount a ON a.UserId=u.Id WHERE a.Kind='Candidate' AND u.Email LIKE '%@example.test'"))==19,"Integration: every fictional candidate links to an actual Identity primary key");
   Assert(Convert.ToInt32(Sql("SELECT COUNT(*) FROM dbo.AspNetUsers u JOIN dbo.DemoAccount a ON a.UserId=u.Id WHERE u.PasswordHash IS NOT NULL"))==0,"Integration: synthetic accounts have no hardcoded/shared password");
   var first=snapshot.Contests.First();int nominee=first.Choices[0].NominationId;
   Assert(!service.Cast(actor,nominee).Success,"Integration: administrator is not a simulated eligible student");
   Assert(!service.Cast("unknown@example.test",nominee).Success,"Integration: unknown or non-synthetic voter rejected");
   Assert(!service.Cast("demo.voter.001",nominee).Success,"Integration: draft election rejects voting");
   service.Schedule(actor,DateTime.Now.AddDays(1),DateTime.Now.AddDays(2));Reject(()=>service.Open(actor),"Integration: cannot open election before its start time");service.Reset(actor);
   service.Schedule(actor,DateTime.Now.AddMinutes(-1),DateTime.Now.AddHours(1));service.Open(actor);
   Assert(service.Cast("demo.candidate.01",nominee).Success,"Integration: candidate accounts are eligible voters");
   Assert(!service.Cast("demo.candidate.01",first.Choices[1].NominationId).Success,"Integration: duplicate vote rejected across different choices in same contest");
   var parallel=Task.WhenAll(Task.Run(()=>service.Cast("demo.candidate.02",nominee)),Task.Run(()=>service.Cast("demo.candidate.02",first.Choices[1].NominationId))).GetAwaiter().GetResult();
   Assert(parallel.Count(r=>r.Success)==1,"Integration: concurrent duplicate requests commit exactly one ballot");
   Assert(service.Simulate(actor,2026,"Preset",200)==1200,"Integration: all 1200 preset ballots pass the normal ballot validation service");
   int total=service.Snapshot(actor).Ballots;Assert(service.Simulate(actor,2026,"Preset",200)==0&&service.Snapshot(actor).Ballots==total,"Integration: repeat simulation cannot duplicate participation");
   Assert(service.Snapshot(publishedOnly:true).Contests.Count==0,"Integration: unpublished tally is hidden from public results");
   Assert(Convert.ToInt32(Sql("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='DemoBallot' AND COLUMN_NAME IN ('UserId','username','ParticipationId','Timestamp')"))==0,"Integration: anonymous choices contain no voter/receipt/timestamp link");
   Assert(Convert.ToInt32(Sql("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='DemoParticipation' AND COLUMN_NAME IN ('NominationId','candidateStructureId','BallotId')"))==0,"Integration: participation contains no ballot choice link");
   Assert(Convert.ToInt32(Sql("SELECT (SELECT COUNT(*) FROM dbo.userVotes)+(SELECT COUNT(*) FROM dbo.Votes)"))==0,"Integration: legacy voter-choice tables remain empty in the demo");
   Sql("UPDATE dbo.VotingSession SET endDate=DATEADD(minute,-1,GETDATE()) WHERE Id="+first.SessionId);Assert(!service.Cast("demo.candidate.03",nominee).Success,"Integration: expired period rejects votes even if phase still says Open");Sql("UPDATE dbo.VotingSession SET endDate=DATEADD(hour,1,GETDATE()) WHERE Id="+first.SessionId);
   service.Close(actor);Assert(!service.Cast("demo.candidate.03",nominee).Success,"Integration: closed contest rejects votes");
   service.Publish(actor);snapshot=service.Snapshot(publishedOnly:true);Assert(snapshot.Published&&snapshot.Contests.First().Allocation.Winners[0].Name=="Tatenda Moyo"&&snapshot.Contests.First().Allocation.Winners[1].Name=="Rumbidzai Ndlovu","Integration: persisted presidential totals publish President and Vice President correctly");
   string adminHash=Convert.ToString(Sql("SELECT PasswordHash FROM dbo.AspNetUsers WHERE Id=(SELECT AdminUserId FROM dbo.DemoInstallation WHERE Id=1)"));
   service.Reset(actor);snapshot=service.Snapshot(actor);Assert(snapshot.Ballots==0&&snapshot.Contests.Count==6&&snapshot.Contests.All(c=>c.Phase=="Draft")&&snapshot.Eligible==219,"Integration: backed-up reset clears votes but preserves synthetic inventory");
   Assert(Convert.ToString(Sql("SELECT PasswordHash FROM dbo.AspNetUsers WHERE Id=(SELECT AdminUserId FROM dbo.DemoInstallation WHERE Id=1)"))==adminHash,"Integration: reset preserves existing administrator authentication");
   Start("Random");var randomVotes=service.Snapshot(actor).Contests.SelectMany(c=>c.Choices.Select(v=>v.Votes)).ToArray();service.Reset(actor);Start("Random");Assert(randomVotes.SequenceEqual(service.Snapshot(actor).Contests.SelectMany(c=>c.Choices.Select(v=>v.Votes))),"Integration: random simulation is reproducible for a fixed seed");
   service.Reset(actor);Start("Tie");Assert(service.Snapshot(actor).Contests.All(c=>c.Allocation.TiedNominees.Any()),"Integration: tie distribution creates six unresolved contests");Reject(()=>service.Publish(actor),"Integration: unresolved ties block publication");
   foreach(var contest in service.Snapshot(actor).Contests)service.CreateRunoff(actor,contest.SessionId);
   Assert(service.Snapshot(actor).Contests.Count==12,"Integration: administrator-approved runoffs contain only tied nominees");
   service.Schedule(actor,DateTime.Now.AddMinutes(-1),DateTime.Now.AddHours(1));service.Open(actor);service.Simulate(actor,2026,"Preset",200);service.Close(actor);service.Publish(actor);
   Assert(service.Snapshot(publishedOnly:true).Contests.Where(c=>!c.ParentSessionId.HasValue).Sum(c=>c.Allocation.Winners.Count)==7,"Integration: runoff resolution allocates seven offices across six contests");
   service.Reset(actor);Start("ViceTie");var presidential=service.Snapshot(actor).Contests.First();Assert(presidential.Allocation.Winners.Single().Name=="Tatenda Moyo"&&presidential.Allocation.PendingSeat=="Vice President","Integration: persisted second-place tie preserves the elected President");service.CreateRunoff(actor,presidential.SessionId);
   var runoff=service.Snapshot(actor).Contests.Single(c=>c.ParentSessionId.HasValue);Assert(runoff.Seats==1&&runoff.SeatLabel=="Vice President"&&runoff.Choices.Count==2,"Integration: Vice President runoff has one seat and only tied candidates");
   service.Schedule(actor,DateTime.Now.AddMinutes(-1),DateTime.Now.AddHours(1));service.Open(actor);service.Simulate(actor,9,"Preset",200);service.Close(actor);service.Publish(actor);
   Assert(service.Snapshot(publishedOnly:true).Contests.First().Allocation.Winners.Count==2,"Integration: Vice President runoff does not replace the President");
   Sql("INSERT dbo.AspNetUsers(Id,UserName,Email,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount) VALUES(N'unexpected-test-only',N'unexpected.synthetic',N'unexpected@example.test',0,0,0,0,0)");
   Reject(()=>service.Reset(actor),"Integration: unexpected non-demo account blocks reset before data deletion");Sql("DELETE dbo.AspNetUsers WHERE Id=N'unexpected-test-only'");
   service.Reset(actor);Assert(service.Snapshot(actor).Ballots==0,"Integration: repeat demonstrations can restart safely");
   Console.WriteLine("SUCCESS: "+count+" demo unit/integration checks passed. All election data used was synthetic.");return 0;
  }catch(Exception e){Console.Error.WriteLine(e.GetBaseException().Message);return 1;}
 }
}

