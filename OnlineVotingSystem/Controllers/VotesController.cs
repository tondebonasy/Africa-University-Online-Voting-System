using System.Threading.Tasks;
using System.Web.Mvc;
using OnlineVotingSystem.Services;
namespace OnlineVotingSystem.Controllers
{
    [Authorize]
    public class VotesController:Controller
    {
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<ActionResult> Vote(int? id)
        {
            if(!id.HasValue)return new HttpStatusCodeResult(400);
            var result=await new BallotService().CastAsync(User.Identity.Name,id.Value);
            TempData["VoteError"]=!result.Success;TempData["VoteMessage"]=result.Message;
            return RedirectToAction("Receipt");
        }
        [HttpGet]public ActionResult Receipt(){return View("Vote");}
    }
}
