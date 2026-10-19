using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace OnlineVotingSystem.Models
{
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    public class VotingSessionsController : Controller
    {
        private OnlineVotingSystemEntities db = new OnlineVotingSystemEntities();

        // GET: VotingSessions
        public ActionResult Index()
        {
            return View(db.VotingSessions.ToList());
        }

        // GET: VotingSessions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VotingSession votingSession = db.VotingSessions.Find(id);
            if (votingSession == null)
            {
                return HttpNotFound();
            }
            return View(votingSession);
        }

        // GET: VotingSessions/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: VotingSessions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,createdBy,startDay,endDate,status")] VotingSession votingSession)
        {

            if (!votingSession.startDay.HasValue || !votingSession.endDate.HasValue || votingSession.endDate <= votingSession.startDay)
                ModelState.AddModelError("endDate", "Choose an end date after the start date.");
            if (votingSession.startDay.HasValue && votingSession.endDate.HasValue && db.VotingSessions.Any(s => s.Id != votingSession.Id && s.startDay < votingSession.endDate && s.endDate > votingSession.startDay))
                ModelState.AddModelError("", "Election dates overlap an existing session.");
            votingSession.createdBy = User.Identity.Name;
            if (ModelState.IsValid)
            {
                if (ModelState.IsValid)
                {
                    db.VotingSessions.Add(votingSession);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            return View(votingSession);
        }

        // GET: VotingSessions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VotingSession votingSession = db.VotingSessions.Find(id);
            if (votingSession == null)
            {
                return HttpNotFound();
            }
            return View(votingSession);
        }

        // POST: VotingSessions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,createdBy,startDay,endDate,status")] VotingSession votingSession)
        {
            if (!votingSession.startDay.HasValue || !votingSession.endDate.HasValue || votingSession.endDate <= votingSession.startDay)
                ModelState.AddModelError("endDate", "Choose an end date after the start date.");
            if (votingSession.startDay.HasValue && votingSession.endDate.HasValue && db.VotingSessions.Any(s => s.Id != votingSession.Id && s.startDay < votingSession.endDate && s.endDate > votingSession.startDay))
                ModelState.AddModelError("", "Election dates overlap an existing session.");
            votingSession.createdBy = User.Identity.Name;
            if (ModelState.IsValid)
            {
                db.Entry(votingSession).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(votingSession);
        }

        // GET: VotingSessions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VotingSession votingSession = db.VotingSessions.Find(id);
            if (votingSession == null)
            {
                return HttpNotFound();
            }
            return View(votingSession);
        }

        // POST: VotingSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            VotingSession votingSession = db.VotingSessions.Find(id);
            db.VotingSessions.Remove(votingSession);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}



