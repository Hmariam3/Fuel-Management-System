using System;
using System.Linq;
using System.Web.Mvc;
using FuelManagementSystem.Models;
using FuelManagementSystem.Services; // For TotalCardService
using System.Threading.Tasks;
using System.Data.Entity;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace FuelManagementSystem.Controllers
{
    public class EcardReplenishmentRequestsController : BaseController
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();
        private readonly TotalCardService totalCardService;

        public EcardReplenishmentRequestsController(TotalCardService service)
        {
            totalCardService = service;
        }

        // GET: Requests
        public ActionResult Index()
        {
            var requests = db.EcardReplenishmentRequests
                .Include(e => e.Ecard.Vehicle.Driver)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();
            return View(requests);
        }

        // GET: Requests/Create
        public ActionResult Create(string ecardId)
        {
            ViewBag.EcardID = new SelectList(db.Ecards.Where(e => e.Status == "Active"), "EcardID", "EcardID", ecardId);
            return View();
        }

        // POST: Requests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(EcardReplenishmentRequest request)
        {
            if (ModelState.IsValid)
            {
                // 1. Fetch card
                var ecard = db.Ecards.Find(request.EcardID);
                if (ecard == null)
                {
                    ModelState.AddModelError("EcardID", "Invalid Ecard selected.");
                    ViewBag.EcardID = new SelectList(db.Ecards, "EcardID", "EcardID", request.EcardID);
                    return View(request);
                }

                // 2. Fetch real-time balance from API
                decimal currentBalance = ecard.Balance; // fallback to DB balance
                try
                {
                    if (await totalCardService.LoginAsync("ETH02542", "H8KJ8PZH")) // TODO: secure credentials
                    {
                        var apiResponse = await totalCardService.GetApiTransactionsAsync(
                            ecard.EcardID,
                            DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
                            DateTime.Now.ToString("yyyy-MM-dd"));

                        currentBalance = (decimal?)(apiResponse?.Data?
                            .OrderByDescending(t => t.TransactionDateTime)
                            .FirstOrDefault()?.Solde) ?? currentBalance;
                    }
                }
                catch (Exception ex)
                {
                    // log exception, keep fallback
                    System.Diagnostics.Debug.WriteLine("Balance sync failed: " + ex.Message);
                }

                // 3. Update Ecard table with the fresh balance
                ecard.Balance = currentBalance;
                db.Entry(ecard).State = EntityState.Modified;

                // 4. Populate replenishment request
                request.RequestedAt = DateTime.Now;
                request.Status = "Pending";
                request.RequestedBy = Session["UserId"] != null ? Session["UserId"].ToString() : null;
                request.CurrentBalance = currentBalance;
                request.BalanceAfter = null;

                db.EcardReplenishmentRequests.Add(request);

                // 5. Save both request + balance update
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.EcardID = new SelectList(db.Ecards, "EcardID", "EcardID", request.EcardID);
            return View(request);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            var request = db.EcardReplenishmentRequests.Find(id);
            if (request == null) return HttpNotFound();

            request.Status = "Approved";
            request.BalanceAfter = request.CurrentBalance + request.RequestedAmount;

            db.SaveChanges();
            return RedirectToAction("Index");
        }


        // POST: Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string note)
        {
            var request = db.EcardReplenishmentRequests.Find(id);
            if (request == null) return HttpNotFound();

            request.Status = "Rejected";
            request.Note = note;
            db.SaveChanges();

            return RedirectToAction("Index");
        }


        //Export to PDFs 
        public ActionResult ExportApprovedToPdf()
        {
            var approvedRequests = db.EcardReplenishmentRequests
                .Where(r => r.Status == "Approved")
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            if (!approvedRequests.Any())
            {
                TempData["Error"] = "No approved requests found to export.";
                return RedirectToAction("Index");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                var pdfDoc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                pdfDoc.Add(new Paragraph("Approved E-Card Replenishment Requests", titleFont));
                pdfDoc.Add(new Paragraph("Generated at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
                pdfDoc.Add(new Paragraph(" "));

                // Table
                PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 12, 15, 15, 15, 20, 20 });

                // Headers
                string[] headers = { "Ecard ID", "Plate No", "Current Balance", "Requested Amount", "Balance After", "Approved At" };
                foreach (var header in headers)
                {
                    table.AddCell(new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });
                }

                // Data rows
                foreach (var r in approvedRequests)
                {
                    table.AddCell(r.EcardID);
                    table.AddCell(r.Ecard?.PlateNo ?? "-");
                    table.AddCell(r.CurrentBalance.GetValueOrDefault().ToString("N2"));
                    table.AddCell(r.RequestedAmount.GetValueOrDefault().ToString("N2"));
                    table.AddCell(r.BalanceAfter.GetValueOrDefault().ToString("N2"));
                    table.AddCell(r.RequestedAt.HasValue ? r.RequestedAt.Value.ToString("yyyy-MM-dd HH:mm") : "-");
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(ms.ToArray(), "application/pdf", "Approved_Replenishment_Requests.pdf");
            }
        }

        // GET: EcardReplenishmentRequests/GetPendingReplenishments
        public ActionResult GetPendingReplenishments()
        {
            try
            {
                var pendingCount = db.EcardReplenishmentRequests.Count(r => r.Status == "Pending");
                return Json(new { count = pendingCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: EcardReplenishmentRequests/GetPendingReplenishments (Detailed for To-Do List)
        public ActionResult GetPendingReplenishmentsDetails()
        {
            try
            {
                var pendingReplenishments = db.EcardReplenishmentRequests
                    .Where(r => r.Status == "Pending")
                    .OrderBy(r => r.RequestedAt)
                    .Take(5) // Adjust the number of tasks as needed
                    .Select(r => new
                    {
                        r.Id,
                        r.EcardID,
                        r.RequestedAt,
                        Status = r.Status
                    })
                    .ToList();
                return Json(pendingReplenishments, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new[] { new { Id = 0, EcardID = "N/A", RequestedAt = DateTime.Now, Status = "Pending" } }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
