using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FuelManagementSystem.Models;

namespace FuelManagementSystem.Controllers
{
    public class UserOrgansController : Controller
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: UserOrgans
        public ActionResult Index()
        {
            return View(db.UserOrgans.ToList());
        }

        // GET: UserOrgans/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserOrgan userOrgan = db.UserOrgans.Find(id);
            if (userOrgan == null)
            {
                return HttpNotFound();
            }
            return View(userOrgan);
        }

        // GET: UserOrgans/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserOrgans/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,UserOrgan1")] UserOrgan userOrgan)
        {
            if (ModelState.IsValid)
            {
                db.UserOrgans.Add(userOrgan);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(userOrgan);
        }

        // GET: UserOrgans/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserOrgan userOrgan = db.UserOrgans.Find(id);
            if (userOrgan == null)
            {
                return HttpNotFound();
            }
            return View(userOrgan);
        }

        // POST: UserOrgans/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,UserOrgan1")] UserOrgan userOrgan)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userOrgan).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(userOrgan);
        }

        // GET: UserOrgans/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserOrgan userOrgan = db.UserOrgans.Find(id);
            if (userOrgan == null)
            {
                return HttpNotFound();
            }
            return View(userOrgan);
        }

        // POST: UserOrgans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            UserOrgan userOrgan = db.UserOrgans.Find(id);
            db.UserOrgans.Remove(userOrgan);
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
