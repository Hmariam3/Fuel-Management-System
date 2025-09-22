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
    public class FuelTransactionsController : Controller
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: FuelTransactions
        public ActionResult Index()
        {
            var fuelTransactions = db.FuelTransactions.Include(f => f.Driver).Include(f => f.FuelRefillRequest).Include(f => f.Vehicle);
            return View(fuelTransactions.ToList());
        }

        // GET: FuelTransactions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelTransaction fuelTransaction = db.FuelTransactions.Find(id);
            if (fuelTransaction == null)
            {
                return HttpNotFound();
            }
            return View(fuelTransaction);
        }

        // GET: FuelTransactions/Create
        public ActionResult Create()
        {
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName");
            ViewBag.RequestID = new SelectList(db.FuelRefillRequests, "RequestID", "PlateNo");
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "VehicleType");
            return View();
        }

        // POST: FuelTransactions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TransactionID,RequestID,PlateNo,DriverID,LitersUsed,Amount,BegKm,EndKm,D_KM,AvgKmLiter,TransactionDateTime,EcardBalanceAfter,Location,ReceiptNum,Product")] FuelTransaction fuelTransaction)
        {
            if (ModelState.IsValid)
            {
                db.FuelTransactions.Add(fuelTransaction);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelTransaction.DriverID);
            ViewBag.RequestID = new SelectList(db.FuelRefillRequests, "RequestID", "PlateNo", fuelTransaction.RequestID);
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "VehicleType", fuelTransaction.PlateNo);
            return View(fuelTransaction);
        }

        // GET: FuelTransactions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelTransaction fuelTransaction = db.FuelTransactions.Find(id);
            if (fuelTransaction == null)
            {
                return HttpNotFound();
            }
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelTransaction.DriverID);
            ViewBag.RequestID = new SelectList(db.FuelRefillRequests, "RequestID", "PlateNo", fuelTransaction.RequestID);
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "VehicleType", fuelTransaction.PlateNo);
            return View(fuelTransaction);
        }

        // POST: FuelTransactions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TransactionID,RequestID,PlateNo,DriverID,LitersUsed,Amount,BegKm,EndKm,D_KM,AvgKmLiter,TransactionDateTime,EcardBalanceAfter,Location,ReceiptNum,Product")] FuelTransaction fuelTransaction)
        {
            if (ModelState.IsValid)
            {
                db.Entry(fuelTransaction).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelTransaction.DriverID);
            ViewBag.RequestID = new SelectList(db.FuelRefillRequests, "RequestID", "PlateNo", fuelTransaction.RequestID);
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "VehicleType", fuelTransaction.PlateNo);
            return View(fuelTransaction);
        }

        // GET: FuelTransactions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelTransaction fuelTransaction = db.FuelTransactions.Find(id);
            if (fuelTransaction == null)
            {
                return HttpNotFound();
            }
            return View(fuelTransaction);
        }

        // POST: FuelTransactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            FuelTransaction fuelTransaction = db.FuelTransactions.Find(id);
            db.FuelTransactions.Remove(fuelTransaction);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: FuelTransactions/GetTotalTransactions
        public ActionResult GetTotalTransactions()
        {
            try
            {
                var count = db.FuelTransactions.Count();
                return Json(new { count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: FuelTransactions/GetRecentTransactions
        public ActionResult GetRecentTransactions()
        {
            try
            {
                var recentTransactions = db.FuelTransactions
                    .OrderByDescending(t => t.TransactionDateTime)
                    .Take(5) // Adjust the number of recent records as needed
                    .Select(t => new
                    {
                        t.TransactionID,
                        t.TransactionDateTime,
                        t.PlateNo,
                        t.LitersUsed,
                        t.Amount
                    })
                    .ToList();
                return Json(recentTransactions, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new[] { new { TransactionID = 0, TransactionDateTime = DateTime.Now, PlateNo = "N/A", LitersUsed = 0m, Amount = 0m } }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: FuelTransactions/GetFuelUsageDistribution
        public ActionResult GetFuelUsageDistribution()
        {
            try
            {
                var fuelUsage = db.FuelTransactions
                    .GroupBy(t => t.Product)
                    .Select(g => new
                    {
                        Product = g.Key ?? "Unknown",
                        TotalLiters = g.Sum(t => t.LitersUsed)
                    })
                    .ToList();

                var labels = fuelUsage.Select(f => f.Product).ToArray();
                var values = fuelUsage.Select(f => f.TotalLiters).ToArray();

                return Json(new { labels, values }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { labels = new[] { "Unknown" }, values = new[] { 0m } }, JsonRequestBehavior.AllowGet);
            }
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
