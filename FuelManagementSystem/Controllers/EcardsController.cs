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
        public ActionResult Create([Bind(Include = "EcardID,PlateNo,CardType,BalanceType,Balance,Status,ActivationDateTime")] Ecard ecard)
        {
            if (ModelState.IsValid)
            {
                // Check unique EcardID
                if (db.Ecards.Any(e => e.EcardID == ecard.EcardID))
                {
                    ModelState.AddModelError("EcardID", "E-card ID already exists.");
                    ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "MakeAndType", ecard.PlateNo);
                    return View(ecard);
                }

                // Reserved Card: PlateNo should be NULL
                if (ecard.CardType == "Reserved")
                {
                    ecard.PlateNo = null;
                    ecard.Balance = ecard.BalanceType; // Reserved cards usually start with 0 balance
                }
                else // Normal Card
                {
                    // Balance must be exactly 50,000 or 100,000 ETB
                    if (ecard.BalanceType != 50000 && ecard.BalanceType != 100000)
                    {
                        ModelState.AddModelError("Balance", "Fund amount must be either 50,000 or 100,000 ETB.");
                        ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "MakeAndType", ecard.PlateNo);
                        return View(ecard);
                    }

                    // Balance Card: PlateNo should be NULL
                    if (ecard.Balance == null)
                    {
                        ecard.Balance = ecard.BalanceType; // assign Balance type for ecard balance
                    }

                }

                // Set defaults
                ecard.ActivationDateTime = DateTime.Now.AddHours(24); // 24-hour SLA
                ecard.Status = "Active";

                db.Ecards.Add(ecard);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "MakeAndType", ecard.PlateNo);
            return View(ecard);
        }

        // POST: Ecards/MarkAsLost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsLost(string ecardId)
        {
            var card = db.Ecards.FirstOrDefault(e => e.EcardID == ecardId);
            if (card == null)
            {
                return HttpNotFound("Card not found.");
            }

            if (card.Status == "Active")
            {
                card.Status = "Lost";
                db.Entry(card).State = EntityState.Modified;

                //// Add history
                //db.CardAssignmentHistories.Add(new CardAssignmentHistory
                //{
                //    EcardID = card.EcardID,
                //    PlateNo = card.PlateNo,
                //    AssignedAt = DateTime.Now,
                //    Action = "Lost"
                //});

                db.SaveChanges();

                TempData["Message"] = $"Card {ecardId} marked as Lost.";
            }
            else
            {
                TempData["Error"] = "Only active cards can be marked as lost.";
            }

            return RedirectToAction("Index");
        }


        // POST: Ecards/RestoreLostCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RestoreLostCard(string ecardId)
        {
            var card = db.Ecards.FirstOrDefault(e => e.EcardID == ecardId);
            if (card == null)
            {
                return HttpNotFound("Card not found.");
            }

            if (card.Status != "Lost")
            {
                TempData["Error"] = "Only lost cards can be restored.";
                return RedirectToAction("Index");
            }

            // 1. Restore lost card to Active
            card.Status = "Active";
            db.Entry(card).State = EntityState.Modified;

            // 2. Find the reserved card currently assigned to this vehicle
            var reservedCard = db.Ecards
                .FirstOrDefault(e => e.PlateNo == card.PlateNo && e.CardType == "Reserved" && e.Status == "Active");

            if (reservedCard != null)
            {
                reservedCard.PlateNo = null; // release back to pool
                db.Entry(reservedCard).State = EntityState.Modified;

                // Log release history
                db.CardAssignmentHistories.Add(new CardAssignmentHistory
                {
                    EcardID = reservedCard.EcardID,
                    PlateNo = card.PlateNo,
                    ReleasedAt = DateTime.Now,
                    Action = "Released"
                });
            }

            //// 3. Log restore history for the lost card
            //db.CardAssignmentHistories.Add(new CardAssignmentHistory
            //{
            //    EcardID = card.EcardID,
            //    PlateNo = card.PlateNo,
            //    AssignedAt = DateTime.Now,
            //    Action = "Restored"
            //});

            db.SaveChanges();

            TempData["Message"] = $"Card {ecardId} restored to Active and reserved card released (if any).";
            return RedirectToAction("Index");
        }

        // GET: Ecards/AssignReservedCard
        public ActionResult AssignReservedCard(string ecardid)
        {

            var availableReservedCards = db.Ecards.Find(ecardid);

            // Vehicles that already have a normal active card should NOT appear in the list
            var vehicles = db.Vehicles
                .Where(v => db.Ecards.Any(e => e.PlateNo == v.PlateNo && e.CardType == "Normal" && e.Status == "Lost"))
                .Select(v => new { v.PlateNo, v.MakeAndType })
                .ToList();

            ViewBag.VehiclePlateNo = new SelectList(vehicles, "PlateNo", "PlateNo");
            return View(availableReservedCards);
        }

        // POST: Ecards/AssignReservedCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignReservedCard(string ecardId, string vehiclePlateNo)
        {
            if (string.IsNullOrEmpty(vehiclePlateNo))
            {
                ModelState.AddModelError("", "Please select a vehicle.");
            }

            var reservedCard = db.Ecards.FirstOrDefault(e => e.EcardID == ecardId);
            if (reservedCard == null || reservedCard.CardType != "Reserved")
            {
                return HttpNotFound("Reserved card not found.");
            }

            reservedCard.PlateNo = vehiclePlateNo;
            db.Entry(reservedCard).State = EntityState.Modified;


            // Add history
            db.CardAssignmentHistories.Add(new CardAssignmentHistory
            {
                EcardID = reservedCard.EcardID,
                PlateNo = vehiclePlateNo,
                AssignedAt = DateTime.Now,
                Action = "Assigned"
            });


            db.SaveChanges();

            TempData["Message"] = $"Reserved card {ecardId} assigned to vehicle {vehiclePlateNo}.";
            return RedirectToAction("Index");
        }


        // POST: Ecards/ReleaseReservedCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReleaseReservedCard(string ecardId)
        {
            var reservedCard = db.Ecards.FirstOrDefault(e => e.EcardID == ecardId);
            if (reservedCard == null || reservedCard.CardType != "Reserved")
            {
                return HttpNotFound("Reserved card not found.");
            }

            string oldPlateNo = reservedCard.PlateNo;
            reservedCard.PlateNo = null;
            db.Entry(reservedCard).State = EntityState.Modified;

            // Add history
            db.CardAssignmentHistories.Add(new CardAssignmentHistory
            {
                EcardID = reservedCard.EcardID,
                PlateNo = oldPlateNo,
                ReleasedAt = DateTime.Now,
                Action = "Released"
            });

            db.SaveChanges();

            TempData["Message"] = $"Reserved card {ecardId} released back to pool.";
            return RedirectToAction("Index");
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