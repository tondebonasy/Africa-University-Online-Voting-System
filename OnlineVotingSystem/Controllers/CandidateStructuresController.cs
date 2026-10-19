using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OnlineVotingSystem.Models;

namespace OnlineVotingSystem.Controllers
{
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    public class CandidateStructuresController : Controller
    {
        private OnlineVotingSystemEntities db = new OnlineVotingSystemEntities();

        // GET: CandidateStructures
        public ActionResult Index()
        {
            var candidateStructures = db.CandidateStructures.Include(c => c.Candidate).Include(c => c.Structure);
            return View(candidateStructures.ToList());
        }

        // GET: CandidateStructures/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CandidateStructure candidateStructure = db.CandidateStructures.Find(id);
            if (candidateStructure == null)
            {
                return HttpNotFound();
            }
            return View(candidateStructure);
        }

        // GET: CandidateStructures/Create
        public ActionResult Create()
        {
            ViewBag.candidateId = new SelectList(db.Candidates, "id", "name");
            ViewBag.structureId = new SelectList(db.Structures, "id", "name");
            return View();
        } 

        // POST: CandidateStructures/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "candidateId,structureId,id")] CandidateStructure candidateStructure)
        {
            if (ModelState.IsValid)
            {
                db.CandidateStructures.Add(candidateStructure);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.candidateId = new SelectList(db.Candidates, "id", "name", candidateStructure.candidateId);
            ViewBag.structureId = new SelectList(db.Structures, "id", "name", candidateStructure.structureId);
            return View(candidateStructure);
        }

        // GET: CandidateStructures/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CandidateStructure candidateStructure = db.CandidateStructures.Find(id);
            if (candidateStructure == null)
            {
                return HttpNotFound();
            }
            ViewBag.candidateId = new SelectList(db.Candidates, "id", "name", candidateStructure.candidateId);
            ViewBag.structureId = new SelectList(db.Structures, "id", "name", candidateStructure.structureId);
            return View(candidateStructure);
        }

        // POST: CandidateStructures/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "candidateId,structureId,id")] CandidateStructure candidateStructure)
        {
            if (ModelState.IsValid)
            {
                db.Entry(candidateStructure).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.candidateId = new SelectList(db.Candidates, "id", "name", candidateStructure.candidateId);
            ViewBag.structureId = new SelectList(db.Structures, "id", "name", candidateStructure.structureId);
            return View(candidateStructure);
        }

        // GET: CandidateStructures/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CandidateStructure candidateStructure = db.CandidateStructures.Find(id);
            if (candidateStructure == null)
            {
                return HttpNotFound();
            }
            return View(candidateStructure);
        }

        // POST: CandidateStructures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CandidateStructure candidateStructure = db.CandidateStructures.Find(id);
            db.CandidateStructures.Remove(candidateStructure);
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

