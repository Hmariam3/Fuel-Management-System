using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using FuelManagementSystem.Models;
using System.Web;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;
using FuelManagementSystem.Services;

namespace FuelManagementSystem.Controllers
{
    public class ImportTransactionsController : BaseController
    {
        private readonly FuelManagementSystemEntities db;
        private readonly TotalCardService _totalCardService;

        public ImportTransactionsController(FuelManagementSystemEntities dbContext, TotalCardService totalCardService)
        {
            db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _totalCardService = totalCardService ?? throw new ArgumentNullException(nameof(totalCardService));
        }

        // GET: ImportTransactions
        public ActionResult Index()
        {
            return View();
        }

        // --- CSV Upload remains the same (your existing code is fine) ---

        // GET: ImportTransactions/PreviewApiTransactions
        public async Task<ActionResult> PreviewApiTransactions(string cardId)
        {
            try
            {
                var success = await _totalCardService.LoginAsync("ETH02542", "H8KJ8PZH");
                if (!success)
                {
                    ViewBag.Error = "Login to Total Fuel Card system failed. Check logs for details.";
                    return View("Index");
                }

                var apiResponse = await _totalCardService.GetApiTransactionsAsync(cardId);
                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    ViewBag.Transactions = apiResponse.Data;
                    ViewBag.Message = $"Fetched {apiResponse.Data.Count} transactions from API for preview (Card ID: {cardId}).";
                }
                else
                {
                    ViewBag.Warning = $"No transactions fetched for Card ID {cardId}.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error fetching from API: {ex.Message} at {DateTime.Now}";
                Debug.WriteLine($"Preview API Error: {ex.Message} at {DateTime.Now}");
            }

            return View("Index");
        }

        // POST: ImportTransactions/ImportFromApi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ImportFromApi(string cardId)
        {
            try
            {
                var success = await _totalCardService.LoginAsync("ETH02542", "H8KJ8PZH");
                if (!success)
                {
                    ViewBag.Error = "Login failed.";
                    return View("Index");
                }

                int importedCount = await _totalCardService.ImportToDatabaseAsync(cardId);
                ViewBag.Message = $"Successfully imported {importedCount} transactions from API for Card ID: {cardId}.";
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error importing from API: {ex.Message}";
                Debug.WriteLine($"Import API Error: {ex.Message}");
            }

            return View("Index");
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
