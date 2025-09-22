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
    public class FuelStandardsController : Controller
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: FuelStandards
        public ActionResult Index()
        {
            var fuelStandards = db.FuelStandards.Include(f => f.FuelType);
            return View(fuelStandards.ToList());
        }

        // GET: FuelStandards/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelStandard fuelStandard = db.FuelStandards.Find(id);
            if (fuelStandard == null)
            {
                return HttpNotFound();
            }
            return View(fuelStandard);
        }

        // GET: FuelStandards/Create
        public ActionResult Create()
        {
            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName");
            return View();
        }

        // POST: FuelStandards/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "StandardID,VehicleType,FuelTypeID,StandardKmPerLiter")] FuelStandard fuelStandard)
        {
            if (ModelState.IsValid)
            {
                db.FuelStandards.Add(fuelStandard);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName", fuelStandard.FuelTypeID);
            return View(fuelStandard);
        }

        // GET: FuelStandards/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelStandard fuelStandard = db.FuelStandards.Find(id);
            if (fuelStandard == null)
            {
                return HttpNotFound();
            }
            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName", fuelStandard.FuelTypeID);
            return View(fuelStandard);
        }

        // POST: FuelStandards/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "StandardID,VehicleType,FuelTypeID,StandardKmPerLiter")] FuelStandard fuelStandard)
        {
            if (ModelState.IsValid)
            {
                db.Entry(fuelStandard).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName", fuelStandard.FuelTypeID);
            return View(fuelStandard);
        }

        // GET: FuelStandards/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelStandard fuelStandard = db.FuelStandards.Find(id);
            if (fuelStandard == null)
            {
                return HttpNotFound();
            }
            return View(fuelStandard);
        }

        // POST: FuelStandards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            FuelStandard fuelStandard = db.FuelStandards.Find(id);
            db.FuelStandards.Remove(fuelStandard);
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
