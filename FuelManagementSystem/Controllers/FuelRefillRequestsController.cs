using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FuelManagementSystem.Models;
using FuelManagementSystem.Services; // For TotalCardService
using System.Threading.Tasks;

namespace FuelManagementSystem.Controllers
{
    public class FuelRefillRequestsController : BaseController
    {
        private readonly FuelManagementSystemEntities db;
        private readonly TotalCardService totalCardService;

        public FuelRefillRequestsController(FuelManagementSystemEntities dbContext, TotalCardService totalCardService)
        {
            db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.totalCardService = totalCardService ?? throw new ArgumentNullException(nameof(totalCardService));
        }

        // GET: FuelRefillRequests
        public ActionResult Index()
        {
            var fuelRefillRequests = db.FuelRefillRequests
                .Include(f => f.Driver)
                .Include(f => f.Vehicle.Ecards)
                .Include(f => f.Vehicle);
            return View(fuelRefillRequests.ToList());
        }

        // GET: FuelRefillRequests/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelRefillRequest fuelRefillRequest = db.FuelRefillRequests
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .FirstOrDefault(f => f.RequestID == id);
            if (fuelRefillRequest == null)
            {
                return HttpNotFound();
            }
            return View(fuelRefillRequest);
        }

        // GET: FuelRefillRequests/Create
        public ActionResult Create()
        {
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo");
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName");
            return View();
        }

        //    // POST: FuelRefillRequests/Create
        //    [HttpPost]
        //    [ValidateAntiForgeryToken]
        //    public async Task<ActionResult> Create(
        //        [Bind(Include = "RequestID,PlateNo,DriverID,CurrentOdometer,RequestDateTime,ReceiptImage,OdometerPhoto,DigitalSignature,Status,MileageDeviation")]
        //FuelRefillRequest fuelRefillRequest,
        //        HttpPostedFileBase odometerPhoto,
        //        HttpPostedFileBase receiptImage,
        //        HttpPostedFileBase digitalSignatureFile,
        //        decimal? litersUsed)
        //    {
        //        const decimal LowBalanceThreshold = 5000m;

        //        if (ModelState.IsValid)
        //        {
        //            var vehicle = db.Vehicles.FirstOrDefault(v => v.PlateNo == fuelRefillRequest.PlateNo);
        //            var ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo && e.Status == "Active");


        //            if (vehicle == null)
        //            {
        //                ModelState.AddModelError("PlateNo", "Invalid vehicle.");
        //            }
        //            else if (vehicle.DriverID != fuelRefillRequest.DriverID)
        //            {
        //                ModelState.AddModelError("DriverID", "Selected driver is not assigned to this vehicle.");
        //            }
        //            else
        //            {
        //                // 🔎 Check balance from API

        //                if (ecard != null)
        //                {
        //                    try
        //                    {
        //                        if (await totalCardService.LoginAsync("ETH02542", "H8KJ8PZH")) // TODO: replace with secure credentials
        //                        {
        //                            var apiResponse = await totalCardService.GetApiTransactionsAsync(
        //                                                ecard.EcardID,
        //                                                DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
        //                                                DateTime.Now.ToString("yyyy-MM-dd"));
        //                            var balance = (decimal?)(apiResponse?.Data?
        //                                .OrderByDescending(t => t.TransactionDateTime)
        //                                .FirstOrDefault()?.Solde) ?? 0m;

        //                            if (balance < LowBalanceThreshold)
        //                            {
        //                                ViewBag.LowBalanceWarning = $"⚠️ Card balance is low ({balance:N2}). Please request replenishment.";
        //                            }
        //                        }
        //                        else
        //                        {
        //                            ModelState.AddModelError("", "Unable to authenticate with TOTAL API for balance check.");
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        ModelState.AddModelError("", "Error checking card balance: " + ex.Message);
        //                    }
        //                }

        //                // Calculate mileage deviation
        //                var lastTransaction = db.FuelTransactions
        //                    .Where(t => t.PlateNo == fuelRefillRequest.PlateNo)
        //                    .OrderByDescending(t => t.TransactionDateTime)
        //                    .FirstOrDefault();

        //                var begKm = lastTransaction?.EndKm ?? 0;
        //                var distance = fuelRefillRequest.CurrentOdometer - begKm;
        //                var fuelStandard = db.FuelStandards
        //                    .FirstOrDefault(fs => fs.VehicleType == vehicle.VehicleType && fs.FuelTypeID == vehicle.FuelTypeID);

        //                if (fuelStandard != null && distance > 0)
        //                {
        //                    //decimal actualLiters = litersUsed ?? 50m;
        //                    var apiResponse = await totalCardService.GetApiTransactionsAsync(
        //                                                ecard.EcardID,
        //                                                DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
        //                                                DateTime.Now.ToString("yyyy-MM-dd"));
        //                    decimal actualLiters = (decimal?)(apiResponse?.Data?
        //                                            .OrderByDescending(t => t.TransactionDateTime)
        //                                            .FirstOrDefault()?.Quantity) ?? 0m;
        //                    var expectedLiters = (decimal)distance / fuelStandard.StandardKmPerLiter;
        //                    fuelRefillRequest.MileageDeviation = ((actualLiters - expectedLiters) / expectedLiters) * 100;
        //                    fuelRefillRequest.ReviewStatus = Math.Abs((double)fuelRefillRequest.MileageDeviation) > 5 ? "PendingReview" : null;
        //                }
        //                else
        //                {
        //                    fuelRefillRequest.MileageDeviation = 0;
        //                    fuelRefillRequest.ReviewStatus = null;
        //                }

        //                // File uploads
        //                if (odometerPhoto != null && receiptImage != null && digitalSignatureFile != null)
        //                {
        //                    var uploadsDir = Path.Combine(Server.MapPath("~/Uploads/"));
        //                    Directory.CreateDirectory(uploadsDir);

        //                    var odometerFileName = Guid.NewGuid() + Path.GetExtension(odometerPhoto.FileName);
        //                    var receiptFileName = Guid.NewGuid() + Path.GetExtension(receiptImage.FileName);
        //                    var signatureFileName = Guid.NewGuid() + Path.GetExtension(digitalSignatureFile.FileName);

        //                    odometerPhoto.SaveAs(Path.Combine(uploadsDir, odometerFileName));
        //                    receiptImage.SaveAs(Path.Combine(uploadsDir, receiptFileName));
        //                    digitalSignatureFile.SaveAs(Path.Combine(uploadsDir, signatureFileName));

        //                    fuelRefillRequest.OdometerPhoto = "/Uploads/" + odometerFileName;
        //                    fuelRefillRequest.ReceiptImage = "/Uploads/" + receiptFileName;
        //                    fuelRefillRequest.DigitalSignature = "/Uploads/" + signatureFileName;
        //                }
        //                else
        //                {
        //                    ModelState.AddModelError("", "All three files (odometer photo, receipt image, digital signature) are required.");
        //                }
        //            }

        //            if (ModelState.IsValid)
        //            {
        //                fuelRefillRequest.RequestDateTime = DateTime.Now;
        //                fuelRefillRequest.Status = "Pending";

        //                db.FuelRefillRequests.Add(fuelRefillRequest);
        //                db.SaveChanges();

        //                // Save documents
        //                db.Documents.Add(new Document
        //                {
        //                    DocumentType = "OdometerPhoto",
        //                    EntityType = "FuelRefillRequest",
        //                    EntityID = fuelRefillRequest.RequestID,
        //                    FilePath = fuelRefillRequest.OdometerPhoto,
        //                    FileType = Path.GetExtension(fuelRefillRequest.OdometerPhoto),
        //                    UploadedBy = fuelRefillRequest.DriverID.ToString(),
        //                    CreatedAt = DateTime.Now
        //                });
        //                db.Documents.Add(new Document
        //                {
        //                    DocumentType = "ReceiptImage",
        //                    EntityType = "FuelRefillRequest",
        //                    EntityID = fuelRefillRequest.RequestID,
        //                    FilePath = fuelRefillRequest.ReceiptImage,
        //                    FileType = Path.GetExtension(fuelRefillRequest.ReceiptImage),
        //                    UploadedBy = fuelRefillRequest.DriverID.ToString(),
        //                    CreatedAt = DateTime.Now
        //                });
        //                db.Documents.Add(new Document
        //                {
        //                    DocumentType = "DigitalSignature",
        //                    EntityType = "FuelRefillRequest",
        //                    EntityID = fuelRefillRequest.RequestID,
        //                    FilePath = fuelRefillRequest.DigitalSignature,
        //                    FileType = Path.GetExtension(fuelRefillRequest.DigitalSignature),
        //                    UploadedBy = fuelRefillRequest.DriverID.ToString(),
        //                    CreatedAt = DateTime.Now
        //                });

        //                db.SaveChanges();

        //                return RedirectToAction("Index");
        //            }
        //        }

        //        ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", fuelRefillRequest.PlateNo);
        //        ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelRefillRequest.DriverID);
        //        return View(fuelRefillRequest);
        //    }


        // POST: FuelRefillRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            [Bind(Include = "RequestID,PlateNo,DriverID,CurrentOdometer,RequestDateTime,ReceiptImage,OdometerPhoto,DigitalSignature,Status,MileageDeviation,LittersBought,AmountPaid,UnitPrice,Location,ReceiptNum")]
    FuelRefillRequest fuelRefillRequest,
            HttpPostedFileBase odometerPhoto,
            HttpPostedFileBase receiptImage,
            HttpPostedFileBase digitalSignatureFile)
        {
            if (ModelState.IsValid)
            {
                var vehicle = db.Vehicles.FirstOrDefault(v => v.PlateNo == fuelRefillRequest.PlateNo);
                if (vehicle == null)
                {
                    ModelState.AddModelError("PlateNo", "Invalid vehicle.");
                }
                else if (vehicle.DriverID != fuelRefillRequest.DriverID)
                {
                    ModelState.AddModelError("DriverID", "Selected driver is not assigned to this vehicle.");
                }
                else
                {
                    // Get last transaction to check odometer & distance
                    var lastTransaction = db.FuelTransactions
                        .Where(t => t.PlateNo == fuelRefillRequest.PlateNo)
                        .OrderByDescending(t => t.TransactionDateTime)
                        .FirstOrDefault();

                    var begKm = lastTransaction?.EndKm ?? 0;

                    // Validate odometer progression
                    if (fuelRefillRequest.CurrentOdometer <= begKm)
                    {
                        ModelState.AddModelError("CurrentOdometer", "Current odometer reading must be greater than the last recorded ending km.");
                    }

                    // Get vehicle fuel standard
                    var fuelStandard = db.FuelStandards
                        .FirstOrDefault(fs => fs.VehicleType == vehicle.VehicleType && fs.FuelTypeID == vehicle.FuelTypeID);

                    // Determine if this is an early refill
                    if (lastTransaction != null && fuelStandard != null && lastTransaction.ExpectedRangeKm.HasValue)
                    {
                        var distanceSinceLastRefill = fuelRefillRequest.CurrentOdometer - begKm;

                       // Early refill if less than 80% of expected range used (adjust threshold as needed)
                        fuelRefillRequest.IsEarlyRefill = distanceSinceLastRefill < (lastTransaction.ExpectedRangeKm.Value * 0.8m);
                    }
                    else
                    {
                        fuelRefillRequest.IsEarlyRefill = false;
                    }

                    // Validate price * liters ≈ amount
                    if (fuelRefillRequest.AmountPaid.HasValue &&
                        fuelRefillRequest.LittersBought.HasValue &&
                        fuelRefillRequest.UnitPrice.HasValue)
                    {
                        if (Math.Abs(fuelRefillRequest.AmountPaid.Value -
                                    (fuelRefillRequest.LittersBought.Value * fuelRefillRequest.UnitPrice.Value))
                            > (fuelRefillRequest.AmountPaid.Value * 0.01m))
                        {
                            ModelState.AddModelError("AmountPaid", "Amount paid does not match liters bought multiplied by unit price (within 1% tolerance).");

                            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", fuelRefillRequest.PlateNo);
                            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelRefillRequest.DriverID);
                            return View(fuelRefillRequest);
                        }
                    }

                    // ✅ Check E-Card balance logic
                    var ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo && e.Status == "Active");
                    if (ecard == null)
                    {
                        ModelState.AddModelError("", "No active E-Card found for this vehicle.");

                        ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", fuelRefillRequest.PlateNo);
                        ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelRefillRequest.DriverID);
                        return View(fuelRefillRequest);
                    }

                    decimal amount = fuelRefillRequest.AmountPaid ?? 0;
                    decimal cardBalanceAfter = (decimal)(ecard.Balance - amount);
                    

                    // ❌ 1. Prevent creation if balance would go negative
                    if (cardBalanceAfter < 0)
                    {
                        ModelState.AddModelError("", $"Insufficient balance on the E-Card ({ecard.Balance:N2}). Refill amount ({amount:N2}) exceeds available balance.");

                        ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", fuelRefillRequest.PlateNo);
                        ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelRefillRequest.DriverID);
                        

                        return View(fuelRefillRequest);
                    }

                    // ⚠️ 2. Trigger warning if balance after refill is below 25% of BalanceType
                    if (ecard.BalanceType.HasValue)
                    {
                        decimal lowBalanceThreshold = ecard.BalanceType.Value * 0.25m;

                        if (cardBalanceAfter < lowBalanceThreshold)
                        {
                            ViewBag.LowBalanceWarning =
                                $"⚠️ Warning: After this refill, the E-Card balance will drop to {cardBalanceAfter:N2}. " +
                                $"Please create a <a href='/EcardReplenishmentRequests/Create' class='alert-link text-decoration-underline'>Card Replenishment Request</a>.";
                        }
                    }

                    // ⚠️ 3. Alert if there is a pending Replenishment
                    var pendingReplenishment = db.EcardReplenishmentRequests.Any(r => r.EcardID == ecard.EcardID && r.Status == "Pending");

                    if (pendingReplenishment)
                    {
                        ViewBag.InfoMessage = "⚠️ You have a pending replenishment request. Be cautious of your remaining balance.";
                    }


                    // File uploads (same as your version)
                    if (odometerPhoto != null && receiptImage != null && digitalSignatureFile != null)
                    {
                        var uploadsDir = Path.Combine(Server.MapPath("~/Uploads/"));
                        Directory.CreateDirectory(uploadsDir);

                        var odometerFileName = Guid.NewGuid() + Path.GetExtension(odometerPhoto.FileName);
                        var receiptFileName = Guid.NewGuid() + Path.GetExtension(receiptImage.FileName);
                        var signatureFileName = Guid.NewGuid() + Path.GetExtension(digitalSignatureFile.FileName);

                        odometerPhoto.SaveAs(Path.Combine(uploadsDir, odometerFileName));
                        receiptImage.SaveAs(Path.Combine(uploadsDir, receiptFileName));
                        digitalSignatureFile.SaveAs(Path.Combine(uploadsDir, signatureFileName));

                        fuelRefillRequest.OdometerPhoto = "/Uploads/" + odometerFileName;
                        fuelRefillRequest.ReceiptImage = "/Uploads/" + receiptFileName;
                        fuelRefillRequest.DigitalSignature = "/Uploads/" + signatureFileName;
                    }
                    else
                    {
                        ModelState.AddModelError("", "All three files (odometer photo, receipt image, digital signature) are required.");
                    }
                }

                if (ModelState.IsValid)
                {
                    fuelRefillRequest.RequestDateTime = DateTime.Now;
                    fuelRefillRequest.Status = "Pending";
                    fuelRefillRequest.ReviewStatus = null;

                    db.FuelRefillRequests.Add(fuelRefillRequest);

                    await db.SaveChangesAsync();

                    ViewBag.SuccessMessage = "Fuel refill request successfully created!";

                    // ✅ Return a fresh model to clear all fields
                    var emptyModel = new FuelRefillRequest();

                    ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo");
                    ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName");
                    return View(emptyModel);
                }
            }

            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "PlateNo", fuelRefillRequest.PlateNo);
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelRefillRequest.DriverID);
            return View(fuelRefillRequest);
        }

        // GET: FuelRefillRequests/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelRefillRequest fuelRefillRequest = db.FuelRefillRequests.Find(id);
            if (fuelRefillRequest == null)
            {
                return HttpNotFound();
            }
            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "MakeAndType", fuelRefillRequest.PlateNo);
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelRefillRequest.DriverID);
            return View(fuelRefillRequest);
        }

        // POST: FuelRefillRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestID,PlateNo,DriverID,CurrentOdometer,RequestDateTime,ReceiptImage,OdometerPhoto,DigitalSignature,Status,MileageDeviation")] FuelRefillRequest fuelRefillRequest, HttpPostedFileBase odometerPhoto, HttpPostedFileBase receiptImage, HttpPostedFileBase digitalSignatureFile, decimal? litersUsed)
        {
            if (ModelState.IsValid)
            {
                var vehicle = db.Vehicles.FirstOrDefault(v => v.PlateNo == fuelRefillRequest.PlateNo);
                if (vehicle == null)
                {
                    ModelState.AddModelError("PlateNo", "Invalid vehicle.");
                }
                else if (vehicle.DriverID != fuelRefillRequest.DriverID)
                {
                    ModelState.AddModelError("DriverID", "Selected driver is not assigned to this vehicle.");
                }

                if (vehicle != null)
                {
                    var lastTransaction = db.FuelTransactions
                        .Where(t => t.PlateNo == fuelRefillRequest.PlateNo)
                        .OrderByDescending(t => t.TransactionDateTime)
                        .FirstOrDefault();
                    var fuelStandard = db.FuelStandards
                        .FirstOrDefault(fs => fs.VehicleType == vehicle.VehicleType && fs.FuelTypeID == vehicle.FuelTypeID);
                    if (fuelStandard != null && lastTransaction != null && litersUsed.HasValue)
                    {
                        var distance = fuelRefillRequest.CurrentOdometer - lastTransaction.EndKm;
                        var expectedLiters = distance / fuelStandard.StandardKmPerLiter;
                        fuelRefillRequest.MileageDeviation = ((litersUsed.Value - expectedLiters) / expectedLiters) * 100;
                        if (Math.Abs((double)fuelRefillRequest.MileageDeviation) > 5)
                        {
                            ModelState.AddModelError("", "Mileage deviation exceeds ±5% of standard km/L. Please review.");
                        }
                    }
                }

                if (odometerPhoto != null || receiptImage != null || digitalSignatureFile != null)
                {
                    var uploadsDir = Path.Combine(Server.MapPath("~/Uploads/"));
                    Directory.CreateDirectory(uploadsDir);

                    if (odometerPhoto != null)
                    {
                        var odometerFileName = Guid.NewGuid() + Path.GetExtension(odometerPhoto.FileName);
                        odometerPhoto.SaveAs(Path.Combine(uploadsDir, odometerFileName));
                        fuelRefillRequest.OdometerPhoto = "/Uploads/" + odometerFileName;
                    }
                    if (receiptImage != null)
                    {
                        var receiptFileName = Guid.NewGuid() + Path.GetExtension(receiptImage.FileName);
                        receiptImage.SaveAs(Path.Combine(uploadsDir, receiptFileName));
                        fuelRefillRequest.ReceiptImage = "/Uploads/" + receiptFileName;
                    }
                    if (digitalSignatureFile != null)
                    {
                        var signatureFileName = Guid.NewGuid() + Path.GetExtension(digitalSignatureFile.FileName);
                        digitalSignatureFile.SaveAs(Path.Combine(uploadsDir, signatureFileName));
                        fuelRefillRequest.DigitalSignature = "/Uploads/" + signatureFileName;
                    }
                }

                if (ModelState.IsValid)
                {
                    db.Entry(fuelRefillRequest).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.PlateNo = new SelectList(db.Vehicles, "PlateNo", "MakeAndType", fuelRefillRequest.PlateNo);
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", fuelRefillRequest.DriverID);
            return View(fuelRefillRequest);
        }

        // GET: FuelRefillRequests/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelRefillRequest fuelRefillRequest = db.FuelRefillRequests
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .FirstOrDefault(f => f.RequestID == id);
            if (fuelRefillRequest == null)
            {
                return HttpNotFound();
            }
            return View(fuelRefillRequest);
        }

        // POST: FuelRefillRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            FuelRefillRequest fuelRefillRequest = db.FuelRefillRequests.Find(id);
            db.FuelRefillRequests.Remove(fuelRefillRequest);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: FuelRefillRequests/Approve/5
        public ActionResult Approve(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FuelRefillRequest fuelRefillRequest = db.FuelRefillRequests
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .FirstOrDefault(f => f.RequestID == id);
            if (fuelRefillRequest == null)
            {
                return HttpNotFound();
            }
            if (fuelRefillRequest.Status != "Pending")
            {
                ModelState.AddModelError("", "This request cannot be approved as it is not in 'Pending' status.");
                return View(fuelRefillRequest);
            }
            return View(fuelRefillRequest);
        }

        //// POST: FuelRefillRequests/Approve/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Approve(int id, decimal? litersUsed)
        //{
        //    FuelRefillRequest fuelRefillRequest = db.FuelRefillRequests.Find(id);
        //    if (fuelRefillRequest == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    if (fuelRefillRequest.Status != "Pending")
        //    {
        //        ModelState.AddModelError("", "This request cannot be approved as it is not in 'Pending' status.");
        //        return View(fuelRefillRequest);
        //    }

        //    using (var dbTransaction = db.Database.BeginTransaction())
        //    {
        //        try
        //        {
        //            var vehicle = db.Vehicles.FirstOrDefault(v => v.PlateNo == fuelRefillRequest.PlateNo);
        //            if (vehicle == null)
        //            {
        //                ModelState.AddModelError("", "Invalid vehicle associated with this request.");
        //                return View(fuelRefillRequest);
        //            }

        //            // Find the associated Ecard using PlateNo, assuming one active e-card per vehicle
        //            var ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo && e.Status == "Active");
        //            if (ecard == null)
        //            {
        //                // Fallback to any e-card if no active one is found
        //                ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo);
        //                if (ecard == null)
        //                {
        //                    ModelState.AddModelError("", "No e-card found for this vehicle.");
        //                    return View(fuelRefillRequest);
        //                }
        //            }

        //            // Login to TOTAL API if not already logged in
        //            if (!await totalCardService.LoginAsync("ETH02542", "H8KJ8PZH")) // Replace with actual credentials
        //            {
        //                ModelState.AddModelError("", "Failed to authenticate with TOTAL API.");
        //                return View(fuelRefillRequest);
        //            }

        //            // Fetch latest balance from API
        //            var apiResponse = await totalCardService.GetApiTransactionsAsync(ecard.EcardID, DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd"));
        //            decimal currentBalance = (decimal?)(apiResponse?.Data?.OrderByDescending(t => t.TransactionDateTime)?.FirstOrDefault()?.Solde) ?? 0m;
        //            if (currentBalance < 0)
        //            {
        //                ModelState.AddModelError("", "Insufficient balance on the e-card.");
        //                return View(fuelRefillRequest);
        //            }


        //            var lastApiTx = apiResponse?.Data?.OrderByDescending(t => t.TransactionDateTime).FirstOrDefault();
        //            if (lastApiTx == null)
        //            {
        //                ModelState.AddModelError("", "No recent API transaction found for this card.");
        //                return View(fuelRefillRequest);
        //            }

        //            // Calculate cost
        //            decimal liters = (decimal)lastApiTx.Quantity;
        //            decimal amount = (decimal)lastApiTx.Amount;
        //            decimal cardBalanceAfter = (decimal)lastApiTx.Solde;

        //            if (currentBalance < amount)
        //            {
        //                ModelState.AddModelError("", "Insufficient balance to cover the fuel cost.");
        //                return View(fuelRefillRequest);
        //            }

        //            // Last local transaction
        //            var lastTransaction = db.FuelTransactions
        //                .Where(t => t.PlateNo == fuelRefillRequest.PlateNo)
        //                .OrderByDescending(t => t.TransactionDateTime)
        //                .FirstOrDefault();

        //            var distance = fuelRefillRequest.CurrentOdometer - (lastTransaction?.EndKm ?? 0);
        //            var txDate = lastApiTx.TransactionDateTime;
        //            var receiptNo = lastApiTx.NoTicket;
        //            var product = lastApiTx.Product;
        //            var location = lastApiTx.Lieu;

        //            if (txDate == default(DateTime)) txDate = DateTime.Now; // optional fallback

        //            // Record FuelTransaction
        //            var fuelTransaction = new FuelTransaction
        //            {
        //                RequestID = fuelRefillRequest.RequestID,
        //                PlateNo = fuelRefillRequest.PlateNo,
        //                DriverID = fuelRefillRequest.DriverID,
        //                LitersUsed = liters,
        //                Amount = amount,
        //                BegKm = lastTransaction?.EndKm ?? 0,
        //                EndKm = fuelRefillRequest.CurrentOdometer,
        //                D_KM = distance,
        //                AvgKmLiter = liters > 0 ? distance / liters : 0,
        //                TransactionDateTime = txDate,
        //                EcardBalanceAfter = cardBalanceAfter,
        //                Location = location,
        //                ReceiptNum = (int)receiptNo,
        //                Product = product
        //            };
        //            db.FuelTransactions.Add(fuelTransaction);

        //            // Update Ecard balance
        //            ecard.Balance = cardBalanceAfter;
        //            db.Entry(ecard).State = EntityState.Modified;

        //            // Update FuelRefillRequest
        //            fuelRefillRequest.Status = "Approved";
        //            fuelRefillRequest.ReviewStatus = null;
        //            db.Entry(fuelRefillRequest).State = EntityState.Modified;

        //            await db.SaveChangesAsync();

        //            dbTransaction.Commit();
        //            return RedirectToAction("Index");
        //        }
        //        catch
        //        {
        //            dbTransaction.Rollback();
        //            throw;
        //        }
        //    }
        //}

        // POST: FuelRefillRequests/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(int id)
        {
            var fuelRefillRequest = db.FuelRefillRequests.Find(id);
            if (fuelRefillRequest == null) return HttpNotFound();

            if (fuelRefillRequest.Status != "Pending")
            {
                ModelState.AddModelError("", "This request cannot be approved as it is not in 'Pending' status.");
                return View(fuelRefillRequest);
            }

            using (var dbTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    var vehicle = db.Vehicles.FirstOrDefault(v => v.PlateNo == fuelRefillRequest.PlateNo);
                    if (vehicle == null)
                    {
                        ModelState.AddModelError("", "Invalid vehicle associated with this request.");
                        return View(fuelRefillRequest);
                    }

                    // Get active E-card
                    var ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo && e.Status == "Active");
                    if (ecard == null)
                    {
                        ModelState.AddModelError("", "No active E-card found for this vehicle.");
                        return View(fuelRefillRequest);
                    }

                    decimal liters = fuelRefillRequest.LittersBought ?? 0;
                    decimal amount = fuelRefillRequest.AmountPaid ?? 0;
                    decimal cardBalanceAfter = (decimal)(ecard.Balance - amount);

                    if (cardBalanceAfter < 0)
                    {
                        ModelState.AddModelError("", "Insufficient balance on the e-card.");
                        return View(fuelRefillRequest);
                    }


                    var lastTransaction = db.FuelTransactions
                        .Where(t => t.PlateNo == fuelRefillRequest.PlateNo)
                        .OrderByDescending(t => t.TransactionDateTime)
                        .FirstOrDefault();

                    var distance = fuelRefillRequest.CurrentOdometer - (lastTransaction?.EndKm ?? 0);
                    var txDate = fuelRefillRequest.RequestDateTime;

                    // 1️⃣ Calculate Expected Range for this new refill
                    var fuelStandard = db.FuelStandards
                        .FirstOrDefault(fs => fs.VehicleType == vehicle.VehicleType && fs.FuelTypeID == vehicle.FuelTypeID);
                    
                    decimal? expectedRangeKm = null;
                    if (fuelStandard != null && liters > 0)
                        expectedRangeKm = liters * fuelStandard.StandardKmPerLiter;

                    // 2️⃣ Calculate Mileage Deviation (based on previous expected range)
                    decimal? mileageDeviation = null;
                    if (lastTransaction != null && lastTransaction.ExpectedRangeKm > 0)
                        mileageDeviation = ((distance - lastTransaction.ExpectedRangeKm) / lastTransaction.ExpectedRangeKm) * 100;


                    var begKm = lastTransaction?.EndKm ?? 0;
                    var product = fuelStandard.FuelType.FuelTypeName;
                    decimal? avgKmLiter = null;

                    if (lastTransaction != null && lastTransaction.LitersUsed > 0 && distance > 0)
                    {
                        avgKmLiter = distance / lastTransaction.LitersUsed;
                    }



                    // 3️⃣ Record new FuelTransaction
                    var fuelTransaction = new FuelTransaction
                    {
                        RequestID = fuelRefillRequest.RequestID,
                        PlateNo = fuelRefillRequest.PlateNo,
                        DriverID = fuelRefillRequest.DriverID,
                        LitersUsed = liters,
                        Amount = amount,
                        BegKm = begKm,
                        EndKm = fuelRefillRequest.CurrentOdometer,
                        D_KM = distance,
                        AvgKmLiter = avgKmLiter,
                        TransactionDateTime = txDate,
                        EcardBalanceAfter = cardBalanceAfter,
                        Location = fuelRefillRequest.Location,
                        ReceiptNum = fuelRefillRequest.ReceiptNum,
                        Product = product,
                        ExpectedRangeKm = expectedRangeKm
                    };

                    db.FuelTransactions.Add(fuelTransaction);

                    ecard.Balance = cardBalanceAfter;
                    db.Entry(ecard).State = EntityState.Modified;

                    fuelRefillRequest.Status = "Approved";
                    fuelRefillRequest.MileageDeviation = mileageDeviation;
                    fuelRefillRequest.ReviewStatus = (mileageDeviation.HasValue && mileageDeviation < -5) ? "PendingReview" : null;

                    db.Entry(fuelRefillRequest).State = EntityState.Modified;

                    await db.SaveChangesAsync();
                    dbTransaction.Commit();

                    return RedirectToAction("Index");
                }
                catch
                {
                    dbTransaction.Rollback();
                    throw;
                }
            }
        }

        // GET: FuelRefillRequests/GetPendingRefillRequests
        public ActionResult GetPendingRefillRequests()
        {
            try
            {
                var pendingRequests = db.FuelRefillRequests
                    .Where(r => r.Status != "Completed")
                    .OrderBy(r => r.RequestDateTime)
                    .Take(5) // Adjust the number of tasks as needed
                    .Select(r => new
                    {
                        r.RequestID,
                        r.PlateNo,
                        r.RequestDateTime,
                        r.Status
                    })
                    .ToList();
                return Json(pendingRequests, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new[] { new { RequestID = 0, PlateNo = "N/A", RequestDateTime = DateTime.Now, Status = "Pending" } }, JsonRequestBehavior.AllowGet);
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