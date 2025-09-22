using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using FuelManagementSystem.Models;

namespace FuelManagementSystem.Controllers
{
    public class EcardsController : BaseController
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: Ecards
        public ActionResult Index()
        {
            var ecards = db.Ecards.Include(e => e.Vehicle);
            return View(ecards.ToList());
        }

        // GET: Ecards/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ecard ecard = db.Ecards.Find(id);
            if (ecard == null)
            {
                return HttpNotFound();
            }
            return View(ecard);
        }

        // GET: Ecards/Create
        public ActionResult Create()
        {
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo"); // Use MakeAndType for clarity
            return View();
        }

        // POST: Ecards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EcardID,PlateNo,Balance,Status,ActivationDateTime")] Ecard ecard)
        {
            if (ModelState.IsValid)
            {
                // Validate fund amount (50,000–100,000 ETB)
                if (ecard.Balance < 50000 || ecard.Balance > 100000)
                {
                    ModelState.AddModelError("Balance", "Fund amount must be between 50,000 and 100,000 ETB.");
                    ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "MakeAndType", ecard.PlateNo);
                    return View(ecard);
                }

                // Check if EcardID is unique
                if (db.Ecards.Any(e => e.EcardID == ecard.EcardID))
                {
                    ModelState.AddModelError("EcardID", "E-card ID already exists.");
                    ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "MakeAndType", ecard.PlateNo);
                    return View(ecard);
                }

                // Set activation details
                ecard.ActivationDateTime = DateTime.Now.AddHours(24); // 24-hour SLA
                ecard.Status = "Active";

                // Mock payment processing (no TOTAL card API)
                bool paymentProcessed = ProcessPayment(ecard.EcardID, ecard.Balance);
                if (!paymentProcessed)
                {
                    ModelState.AddModelError("", "Payment processing failed. Please try again.");
                    ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", ecard.PlateNo);
                    return View(ecard);
                }

                db.Ecards.Add(ecard);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", ecard.PlateNo);
            return View(ecard);
        }

        // GET: Ecards/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ecard ecard = db.Ecards.Find(id);
            if (ecard == null)
            {
                return HttpNotFound();
            }
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", ecard.PlateNo);
            return View(ecard);
        }

        // POST: Ecards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EcardID,PlateNo,Balance,Status,ActivationDateTime")] Ecard ecard)
        {
            if (ModelState.IsValid)
            {
                // Validate fund amount (50,000–100,000 ETB)
                if (ecard.Balance < 50000 || ecard.Balance > 100000)
                {
                    ModelState.AddModelError("Balance", "Fund amount must be between 50,000 and 100,000 ETB.");
                    ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", ecard.PlateNo);
                    return View(ecard);
                }

                db.Entry(ecard).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", ecard.PlateNo);
            return View(ecard);
        }

        // GET: Ecards/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ecard ecard = db.Ecards.Find(id);
            if (ecard == null)
            {
                return HttpNotFound();
            }
            return View(ecard);
        }

        // POST: Ecards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Ecard ecard = db.Ecards.Find(id);
            db.Ecards.Remove(ecard);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // Mock payment processing method (no TOTAL card API)
        private bool ProcessPayment(string ecardId, decimal amount)
        {
            // Simulate payment processing (e.g., sending letter to finance)
            // Replace with actual logic if payment system becomes available
            return true; // Mock success
        }


        // GET: Ecards/GetActiveEcards
        public ActionResult GetActiveEcards()
        {
            try
            {
                var count = db.Ecards.Count(e => e.Status == "Active");
                return Json(new { count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
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