using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using OnlineVotingSystem.Services;
namespace OnlineVotingSystem.Controllers
{
    [Authorize(Roles="DEMO_ADMIN"),DemoOnly]
    public sealed class DemoController:Controller
    {
        private readonly DemoElectionService service=new DemoElectionService();
        public ActionResult Index(){return View(service.Snapshot(User.Identity.Name));}
        private ActionResult Complete(Action action,string message)
        {
            try {action();if(message!=null)TempData["DemoMessage"]=message;}
            catch(InvalidOperationException ex){TempData["DemoError"]=ex.Message;}
            return RedirectToAction("Index");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<ActionResult> Seed()
        {
            try {await service.SeedAsync(User.Identity.Name);TempData["DemoMessage"]="DEMO data is ready: 19 fictional candidates and 200 synthetic voters. Existing seed records were not duplicated.";}
            catch(InvalidOperationException ex){TempData["DemoError"]=ex.Message;}
            return RedirectToAction("Index");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Schedule(DateTime? start,DateTime? end)
        {return Complete(()=>{if(!start.HasValue||!end.HasValue)throw new InvalidOperationException("Enter both schedule dates.");service.Schedule(User.Identity.Name,start.Value,end.Value);},"Draft DEMO contests scheduled.");}
        [HttpPost,ValidateAntiForgeryToken]public ActionResult Open(){return Complete(()=>service.Open(User.Identity.Name),"Scheduled DEMO contests are open.");}
        [HttpPost,ValidateAntiForgeryToken]public ActionResult Close(){return Complete(()=>service.Close(User.Identity.Name),"Open DEMO contests closed. Review persisted tallies before publication.");}
        [HttpPost,ValidateAntiForgeryToken]public ActionResult Simulate(int seed=2026,string mode="Preset",int voters=200){return Complete(()=>{var count=service.Simulate(User.Identity.Name,seed,mode,voters);TempData["DemoMessage"]="DEMO / SIMULATED: "+count+" validated synthetic ballots recorded.";},null);}
        [HttpPost,ValidateAntiForgeryToken]public ActionResult Runoff(int session){return Complete(()=>service.CreateRunoff(User.Identity.Name,session),"Administrator-approved runoff created as Draft. Schedule, open, simulate and close it before publishing.");}
        [HttpPost,ValidateAntiForgeryToken]public ActionResult Publish(){return Complete(()=>service.Publish(User.Identity.Name),"DEMO / SIMULATED results published.");}
        [HttpPost,ValidateAntiForgeryToken]public ActionResult Reset(){return Complete(()=>service.Reset(User.Identity.Name),"DEMO ballots and runoffs reset after backup. The existing administrator password and synthetic accounts were preserved.");}
        [HttpPost,ValidateAntiForgeryToken]public ActionResult Backup(){return Complete(()=>{service.Backup(User.Identity.Name);},"Local DEMO database backup completed.");}
    }
    [Authorize,DemoOnly]
    public sealed class DemoPublicController:Controller
    {
        public ActionResult Ballot(){return View(new DemoElectionService().BallotSnapshot());}
        public ActionResult Results(){return View(new DemoElectionService().Snapshot(publishedOnly:true));}
    }
}

