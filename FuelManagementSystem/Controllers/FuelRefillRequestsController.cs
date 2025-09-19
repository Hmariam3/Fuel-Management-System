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

        // POST: FuelRefillRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            [Bind(Include = "RequestID,PlateNo,DriverID,CurrentOdometer,RequestDateTime,ReceiptImage,OdometerPhoto,DigitalSignature,Status,MileageDeviation")]
    FuelRefillRequest fuelRefillRequest,
            HttpPostedFileBase odometerPhoto,
            HttpPostedFileBase receiptImage,
            HttpPostedFileBase digitalSignatureFile,
            decimal? litersUsed)
        {
            const decimal LowBalanceThreshold = 5000m;

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
                    // 🔎 Check balance from API
                    var ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo && e.Status == "Active");
                    if (ecard != null)
                    {
                        try
                        {
                            if (await totalCardService.LoginAsync("ETH02542", "H8KJ8PZH")) // TODO: replace with secure credentials
                            {
                                var apiResponse = await totalCardService.GetApiTransactionsAsync(
                                    ecard.EcardID,
                                    DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
                                    DateTime.Now.ToString("yyyy-MM-dd"));

                                var balance = (decimal?)(apiResponse?.Data?
                                    .OrderByDescending(t => t.TransactionDateTime)
                                    .FirstOrDefault()?.Solde) ?? 0m;

                                if (balance < LowBalanceThreshold)
                                {
                                    ViewBag.LowBalanceWarning = $"⚠️ Card balance is low ({balance:N2}). Please request replenishment.";
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("", "Unable to authenticate with TOTAL API for balance check.");
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", "Error checking card balance: " + ex.Message);
                        }
                    }

                    // Calculate mileage deviation
                    var lastTransaction = db.FuelTransactions
                        .Where(t => t.PlateNo == fuelRefillRequest.PlateNo)
                        .OrderByDescending(t => t.TransactionDateTime)
                        .FirstOrDefault();

                    var begKm = lastTransaction?.EndKm ?? 0;
                    var distance = fuelRefillRequest.CurrentOdometer - begKm;
                    var fuelStandard = db.FuelStandards
                        .FirstOrDefault(fs => fs.VehicleType == vehicle.VehicleType && fs.FuelTypeID == vehicle.FuelTypeID);

                    if (fuelStandard != null && distance > 0)
                    {
                        decimal actualLiters = litersUsed ?? 50m;
                        var expectedLiters = (decimal)distance / fuelStandard.StandardKmPerLiter;
                        fuelRefillRequest.MileageDeviation = ((actualLiters - expectedLiters) / expectedLiters) * 100;
                        fuelRefillRequest.ReviewStatus = Math.Abs((double)fuelRefillRequest.MileageDeviation) > 5 ? "PendingReview" : null;
                    }
                    else
                    {
                        fuelRefillRequest.MileageDeviation = 0;
                        fuelRefillRequest.ReviewStatus = null;
                    }

                    // File uploads
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

                    db.FuelRefillRequests.Add(fuelRefillRequest);
                    db.SaveChanges();

                    // Save documents
                    db.Documents.Add(new Document
                    {
                        DocumentType = "OdometerPhoto",
                        EntityType = "FuelRefillRequest",
                        EntityID = fuelRefillRequest.RequestID,
                        FilePath = fuelRefillRequest.OdometerPhoto,
                        FileType = Path.GetExtension(fuelRefillRequest.OdometerPhoto),
                        UploadedBy = fuelRefillRequest.DriverID.ToString(),
                        CreatedAt = DateTime.Now
                    });
                    db.Documents.Add(new Document
                    {
                        DocumentType = "ReceiptImage",
                        EntityType = "FuelRefillRequest",
                        EntityID = fuelRefillRequest.RequestID,
                        FilePath = fuelRefillRequest.ReceiptImage,
                        FileType = Path.GetExtension(fuelRefillRequest.ReceiptImage),
                        UploadedBy = fuelRefillRequest.DriverID.ToString(),
                        CreatedAt = DateTime.Now
                    });
                    db.Documents.Add(new Document
                    {
                        DocumentType = "DigitalSignature",
                        EntityType = "FuelRefillRequest",
                        EntityID = fuelRefillRequest.RequestID,
                        FilePath = fuelRefillRequest.DigitalSignature,
                        FileType = Path.GetExtension(fuelRefillRequest.DigitalSignature),
                        UploadedBy = fuelRefillRequest.DriverID.ToString(),
                        CreatedAt = DateTime.Now
                    });

                    db.SaveChanges();

                    return RedirectToAction("Index");
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

        // POST: FuelRefillRequests/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(int id, decimal? litersUsed)
        {
            FuelRefillRequest fuelRefillRequest = db.FuelRefillRequests.Find(id);
            if (fuelRefillRequest == null)
            {
                return HttpNotFound();
            }
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

                    // Find the associated Ecard using PlateNo, assuming one active e-card per vehicle
                    var ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo && e.Status == "Active");
                    if (ecard == null)
                    {
                        // Fallback to any e-card if no active one is found
                        ecard = db.Ecards.FirstOrDefault(e => e.PlateNo == vehicle.PlateNo);
                        if (ecard == null)
                        {
                            ModelState.AddModelError("", "No e-card found for this vehicle.");
                            return View(fuelRefillRequest);
                        }
                    }

                    // Login to TOTAL API if not already logged in
                    if (!await totalCardService.LoginAsync("ETH02542", "H8KJ8PZH")) // Replace with actual credentials
                    {
                        ModelState.AddModelError("", "Failed to authenticate with TOTAL API.");
                        return View(fuelRefillRequest);
                    }

                    // Fetch latest balance from API
                    var apiResponse = await totalCardService.GetApiTransactionsAsync(ecard.EcardID, DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd"));
                    decimal currentBalance = (decimal?)(apiResponse?.Data?.OrderByDescending(t => t.TransactionDateTime)?.FirstOrDefault()?.Solde) ?? 0m;
                    if (currentBalance < 0)
                    {
                        ModelState.AddModelError("", "Insufficient balance on the e-card.");
                        return View(fuelRefillRequest);
                    }


                    var lastApiTx = apiResponse?.Data?.OrderByDescending(t => t.TransactionDateTime).FirstOrDefault();
                    if (lastApiTx == null)
                    {
                        ModelState.AddModelError("", "No recent API transaction found for this card.");
                        return View(fuelRefillRequest);
                    }

                    // Calculate cost
                    decimal liters = (decimal)lastApiTx.Quantity;
                    decimal amount = (decimal)lastApiTx.Amount;
                    decimal cardBalanceAfter = (decimal)lastApiTx.Solde;

                    if (currentBalance < amount)
                    {
                        ModelState.AddModelError("", "Insufficient balance to cover the fuel cost.");
                        return View(fuelRefillRequest);
                    }

                    // Last local transaction
                    var lastTransaction = db.FuelTransactions
                        .Where(t => t.PlateNo == fuelRefillRequest.PlateNo)
                        .OrderByDescending(t => t.TransactionDateTime)
                        .FirstOrDefault();

                    var distance = fuelRefillRequest.CurrentOdometer - (lastTransaction?.EndKm ?? 0);

                    // Record FuelTransaction
                    var fuelTransaction = new FuelTransaction
                    {
                        RequestID = fuelRefillRequest.RequestID,
                        PlateNo = fuelRefillRequest.PlateNo,
                        DriverID = fuelRefillRequest.DriverID,
                        LitersUsed = liters,
                        Amount = amount,
                        BegKm = lastTransaction?.EndKm ?? 0,
                        EndKm = fuelRefillRequest.CurrentOdometer,
                        D_KM = distance,
                        AvgKmLiter = liters > 0 ? distance / liters : 0,
                        //TransactionDateTime = lastApiTx.TransactionDateTime ?? DateTime.Now,
                        EcardBalanceAfter = cardBalanceAfter
                    };
                    db.FuelTransactions.Add(fuelTransaction);

                    // Update Ecard balance
                    ecard.Balance = cardBalanceAfter;
                    db.Entry(ecard).State = EntityState.Modified;

                    // Update FuelRefillRequest
                    fuelRefillRequest.Status = "Approved";
                    fuelRefillRequest.ReviewStatus = null;
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