using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OnlineVotingSystem.Models;

namespace OnlineVotingSystem.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new OnlineVotingSystemEntities())
                return View(db.VotingSessions.AsNoTracking().OrderByDescending(s => s.Id).ToList());
        }
        public ActionResult About() { return View(); }
        public ActionResult Contact() { return View(); }
        [Authorize]
        public ActionResult ViewResults()
        {
            using (var db = new OnlineVotingSystemEntities())
            {
                var session = db.VotingSessions.OrderByDescending(s => s.Id).FirstOrDefault();
                ViewBag.Election = session;
                if (session == null) return View(new System.Collections.Generic.List<userVote>());
                return View(db.userVotes.Include(v => v.CandidateStructure.Candidate)
                    .Include(v => v.CandidateStructure.Structure)
                    .Where(v => v.sessionId == session.Id).ToList());
            }
        }
        [Authorize]
        public ActionResult ViewCandidates()
        {
            using (var db = new OnlineVotingSystemEntities())
            {
                var now = DateTime.Now;
                ViewBag.VotingOpen = db.VotingSessions.Any(s => s.startDay <= now && s.endDate > now);
                return View(db.Candidates.Include(c => c.CandidateStructures.Select(cs => cs.Structure)).ToList());
            }
        }
        [Authorize]
        public ActionResult ViewCouncilStructures()
        {
            using (var db = new OnlineVotingSystemEntities())
                return View(db.Structures.AsNoTracking().OrderBy(s => s.rank).ToList());
        }
    }
}


