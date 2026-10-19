using System;
using System.Linq;
using OnlineVotingSystem.Services;
class DemoTool
{
    static int Main(string[] args)
    {
        try
        {
            var service=new DemoElectionService();
            var actor=System.Configuration.ConfigurationManager.AppSettings["Demo:AdminUserName"];
            var command=args.Length==0?"status":args[0];
            if(command=="seed")service.SeedAsync(actor).GetAwaiter().GetResult();
            else if(command=="reset")service.Reset(actor);
            else if(command=="schedule")service.Schedule(actor,DateTime.Now.AddMinutes(-1),DateTime.Now.AddDays(1));
            else if(command=="open")service.Open(actor);
            else if(command=="close")service.Close(actor);
            else if(command=="publish")service.Publish(actor);
            else if(command=="simulate")Console.WriteLine("DEMO synthetic ballots: "+service.Simulate(actor,args.Length>2?int.Parse(args[2]):2026,args.Length>1?args[1]:"Preset",200));
            else if(command=="runoffs")foreach(var contest in service.Snapshot(actor).Contests.Where(c=>c.Phase=="Closed"&&c.Allocation.TiedNominees.Count>0))service.CreateRunoff(actor,contest.SessionId);
            else if(command!="status")throw new InvalidOperationException("Unknown demo command.");
            var snapshot=service.Snapshot(actor);
            Console.WriteLine("DEMO / SIMULATED: "+snapshot.Candidates+" fictional candidates; "+snapshot.Voters+" dedicated voters; "+snapshot.Eligible+" eligible; "+snapshot.Ballots+" anonymous ballots; published="+snapshot.Published);
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine("DEMO operation failed: "+e.GetBaseException().Message);return 1;}
    }
}
