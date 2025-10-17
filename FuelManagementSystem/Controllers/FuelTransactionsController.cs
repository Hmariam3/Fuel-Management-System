using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FuelManagementSystem.Models;
using OfficeOpenXml; // Add this for EPPlus
using System.IO;
using System.Globalization;

namespace FuelManagementSystem.Controllers
{
    public class FuelTransactionsController : BaseController
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


        // GET: FuelTransactions/ExportToExcel
        // GET: FuelTransactions/ExportToExcel
        public ActionResult ExportToExcel(string plateNo = null, string month = null)
        {
            try
            {
                // Parse month parameter (format: YYYY-MM)
                DateTime? startDate = null;
                DateTime? endDate = null;

                if (!string.IsNullOrEmpty(month))
                {
                    if (DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        startDate = new DateTime(parsedDate.Year, parsedDate.Month, 1);
                        endDate = startDate.Value.AddMonths(1).AddDays(-1); // Last day of the month
                    }
                }

                // Build the query
                var query = db.FuelTransactions
                    .Include(f => f.Driver)
                    .Include(f => f.FuelRefillRequest)
                    .Include(f => f.Vehicle)
                    .Include(f => f.Vehicle.Ecards) // Include Ecards for CardType
                    .AsQueryable();

                // Filter by PlateNo if specified
                if (!string.IsNullOrEmpty(plateNo))
                {
                    query = query.Where(f => f.PlateNo == plateNo);
                }

                // Filter by date range if specified
                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(f => f.TransactionDateTime >= startDate.Value &&
                                           f.TransactionDateTime <= endDate.Value);
                }

                var transactions = query.OrderBy(t => t.TransactionDateTime).ToList();

                if (!transactions.Any())
                {
                    // Return empty Excel if no data
                    var emptyExcel = GenerateEmptyExcel(plateNo, month);
                    return File(emptyExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               $"FuelTransactions_{plateNo ?? "All"}_{month ?? "All"}_{DateTime.Now:yyyyMMdd}.xlsx");
                }

                // Generate Excel file
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Fuel Transactions");

                    // Set properties
                    package.Workbook.Properties.Title = "Fuel Transactions Report";
                    package.Workbook.Properties.Company = "Fuel Management System";

                    // Create header row
                    CreateHeaderRow(worksheet);

                    // Add data rows
                    int row = 2;
                    foreach (var transaction in transactions)
                    {
                        AddDataRow(worksheet, transaction, row);
                        row++;
                    }

                    // Auto-fit columns (simplified for EPPlus 4.5.3.3)
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Format header row
                    using (var range = worksheet.Cells[1, 1, 1, 18])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(52, 152, 219));
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    // Add summary section
                    AddSummarySection(worksheet, transactions, row + 2, plateNo, month);

                    // Save to MemoryStream and reset position
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0; // Reset stream to beginning

                    string fileName = $"FuelTransactions_{plateNo ?? "All"}_{month ?? "All"}_{DateTime.Now:yyyyMMdd}.xlsx";

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                // Log the exception with details
                System.Diagnostics.Debug.WriteLine($"Error generating Excel report: {ex.Message}\nStackTrace: {ex.StackTrace}");
                TempData["Error"] = "Error generating Excel report: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        // GET: FuelTransactions/GetPlateNosForExport
        public ActionResult GetPlateNosForExport()
        {
            try
            {
                var plateNos = db.FuelTransactions
                    .Select(t => t.PlateNo)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                return Json(plateNos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }

        // GET: FuelTransactions/GetMonthsForExport
        public ActionResult GetMonthsForExport()
        {
            try
            {
                var months = db.FuelTransactions
                    .Select(t => t.TransactionDateTime.Month)
                    .Distinct()
                    .OrderBy(m => m)
                    .Select(m => new { Month = m, MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m) })
                    .ToList();

                return Json(months, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        private void CreateHeaderRow(ExcelWorksheet worksheet)
        {
            // Headers
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "Transaction ID";
            worksheet.Cells[1, 3].Value = "Plate No";
            worksheet.Cells[1, 4].Value = "Vehicle Type";
            worksheet.Cells[1, 5].Value = "Ecard Type";
            worksheet.Cells[1, 6].Value = "Driver";
            worksheet.Cells[1, 7].Value = "Liters Used";
            worksheet.Cells[1, 8].Value = "Amount (Currency)";
            worksheet.Cells[1, 9].Value = "Beginning KM";
            worksheet.Cells[1, 10].Value = "Ending KM";
            worksheet.Cells[1, 11].Value = "Distance KM";
            worksheet.Cells[1, 12].Value = "Avg KM/Liter";
            worksheet.Cells[1, 13].Value = "Expected Range KM";
            worksheet.Cells[1, 14].Value = "Transaction Date";
            worksheet.Cells[1, 15].Value = "Ecard Balance After";
            worksheet.Cells[1, 16].Value = "Location";
            worksheet.Cells[1, 17].Value = "Receipt Number";
            worksheet.Cells[1, 18].Value = "Product";
        }

        private void AddDataRow(ExcelWorksheet worksheet, FuelTransaction transaction, int row)
        {
            worksheet.Cells[row, 1].Value = row - 1; // Row number
            worksheet.Cells[row, 2].Value = transaction.TransactionID;
            worksheet.Cells[row, 3].Value = transaction.PlateNo;
            worksheet.Cells[row, 4].Value = transaction.Vehicle?.MakeAndType ?? "N/A";

            // Get Ecard Type (prioritize Normal, fallback to Reserved)
            var ecardType = transaction.Vehicle?.Ecards?.FirstOrDefault(e => e.CardType == "Normal")?.CardType ??
                           transaction.Vehicle?.Ecards?.FirstOrDefault(e => e.CardType == "Reserved")?.CardType ?? "No Ecard";
            worksheet.Cells[row, 5].Value = ecardType;

            worksheet.Cells[row, 6].Value = transaction.Driver?.DriverName ?? "N/A";
            worksheet.Cells[row, 7].Value = transaction.LitersUsed;
            worksheet.Cells[row, 8].Value = transaction.Amount;
            worksheet.Cells[row, 9].Value = transaction.BegKm;
            worksheet.Cells[row, 10].Value = transaction.EndKm;
            worksheet.Cells[row, 11].Value = transaction.D_KM;
            worksheet.Cells[row, 12].Value = transaction.AvgKmLiter;
            worksheet.Cells[row, 13].Value = transaction.ExpectedRangeKm;
            worksheet.Cells[row, 14].Value = transaction.TransactionDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cells[row, 15].Value = transaction.EcardBalanceAfter;
            worksheet.Cells[row, 16].Value = transaction.Location ?? "N/A";
            worksheet.Cells[row, 17].Value = transaction.ReceiptNum;
            worksheet.Cells[row, 18].Value = transaction.Product ?? "N/A";

            // Apply number formatting
            worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00"; // Liters
            worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0.00"; // Amount
            worksheet.Cells[row, 12].Style.Numberformat.Format = "#,##0.00"; // Avg KM/Liter
            worksheet.Cells[row, 13].Style.Numberformat.Format = "#,##0"; // Expected Range
            worksheet.Cells[row, 15].Style.Numberformat.Format = "#,##0.00"; // Ecard Balance
        }

        private void AddSummarySection(ExcelWorksheet worksheet, List<FuelTransaction> transactions, int startRow, string plateNo, string month)
        {
            int summaryRow = startRow;

            // Summary title
            worksheet.Cells[summaryRow, 1].Value = "SUMMARY REPORT";
            worksheet.Cells[summaryRow, 1, summaryRow, 18].Merge = true;
            worksheet.Cells[summaryRow, 1].Style.Font.Size = 14;
            worksheet.Cells[summaryRow, 1].Style.Font.Bold = true;
            worksheet.Cells[summaryRow, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            summaryRow++;

            // Filter info
            string filterInfo = $"Report for {(string.IsNullOrEmpty(plateNo) ? "All Vehicles" : $"Vehicle: {plateNo}")}";
            if (!string.IsNullOrEmpty(month))
            {
                if (DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    filterInfo += $" | Month: {parsedDate:MMMM yyyy}";
                }
            }
            else
            {
                filterInfo += " | All Months";
            }
            worksheet.Cells[summaryRow, 1].Value = filterInfo;
            worksheet.Cells[summaryRow, 1, summaryRow, 18].Merge = true;
            worksheet.Cells[summaryRow, 1].Style.Font.Italic = true;
            worksheet.Cells[summaryRow, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            summaryRow += 2;

            // Summary statistics
            var totalTransactions = transactions.Count;
            var totalLiters = transactions.Sum(t => t.LitersUsed);
            var totalAmount = transactions.Sum(t => t.Amount);
            var avgKmPerLiter = transactions.Where(t => t.AvgKmLiter.HasValue).Average(t => t.AvgKmLiter.Value);
            var totalDistance = transactions.Sum(t => t.D_KM);
            var avgTransactionAmount = totalTransactions > 0 ? totalAmount / totalTransactions : 0;

            // Summary headers
            string[] summaryHeaders = { "Total Transactions", "Total Liters Used", "Total Amount Spent", "Average KM/Liter", "Total Distance Traveled", "Average Transaction Amount" };
            var summaryValues = new object[] { totalTransactions, totalLiters, totalAmount, avgKmPerLiter, totalDistance, avgTransactionAmount };

            for (int i = 0; i < summaryHeaders.Length; i++)
            {
                worksheet.Cells[summaryRow, 1].Value = summaryHeaders[i];
                worksheet.Cells[summaryRow, 2].Value = summaryValues[i];

                // Format numbers
                if (i == 1 || i == 2 || i == 5) // Liters, Amount, Avg Transaction Amount
                {
                    worksheet.Cells[summaryRow, 2].Style.Numberformat.Format = "#,##0.00";
                }
                else if (i == 3) // Average KM/Liter
                {
                    worksheet.Cells[summaryRow, 2].Style.Numberformat.Format = "#,##0.00";
                }
                else if (i == 4) // Total Distance
                {
                    worksheet.Cells[summaryRow, 2].Style.Numberformat.Format = "#,##0";
                }

                summaryRow++;
            }

            // Style summary section
            using (var range = worksheet.Cells[startRow, 1, summaryRow - 1, 2])
            {
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(236, 240, 241));
            }
        }

        private byte[] GenerateEmptyExcel(string plateNo, string month)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Fuel Transactions");
                worksheet.Cells[1, 1].Value = "No transactions found for the selected criteria.";
                worksheet.Cells[1, 1, 1, 18].Merge = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                return package.GetAsByteArray();
            }
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
