using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FuelManagementSystem.Models;
using System.Data.Entity;
using System.Configuration;

namespace FuelManagementSystem.Controllers
{
    public class DriversController : BaseController
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: Drivers
        public ActionResult Index()
        {
            var drivers = db.Drivers
                .Include(d => d.User)
                .ToList();
            return View(drivers);
        }

        // GET: Drivers/Create
        public ActionResult Create()
        {
            var driver = new Driver
            {
                CreatedAt = DateTime.Now
            };
            return View(driver);
        }

        // POST: Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Driver driver, HttpPostedFileBase[] documents)
        {
            if (ModelState.IsValid)
            {
                driver.CreatedAt = DateTime.Now;
                driver.UpdatedAt = DateTime.Now;
                driver.CreatedBy = Session["UserId"] != null ? (int?)Convert.ToInt32(Session["UserId"]) : null;

                db.Drivers.Add(driver);
                db.SaveChanges();

                // Handle file upload
                string docPath = Server.MapPath("~/UploadedFiles/InsuranceDocument/Driver/");
                if (!Directory.Exists(docPath))
                    Directory.CreateDirectory(docPath);

                try
                {
                    foreach (var licenseAttachment in documents)
                    {
                        if (licenseAttachment != null && licenseAttachment.ContentLength > 0)
                        {
                            string fileName = $"Driver{driver.DriverID}_LicenseAttachment_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(licenseAttachment.FileName)}";
                            string filePath = Path.Combine(docPath, fileName);
                            licenseAttachment.SaveAs(filePath);
                            //driver.LicenseAttachment = filePath; // Optional: Set if used in model
                            db.Documents.Add(new Document
                            {
                                EntityType = "Driver",
                                EntityID = driver.DriverID,
                                DocumentType = "LicenseAttachment",
                                FilePath = filePath,
                                FileSize = licenseAttachment.ContentLength / 1024, // Integer division
                                FileType = Path.GetExtension(licenseAttachment.FileName).TrimStart('.'),
                                UploadedBy = User.Identity.Name,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            });
                            db.SaveChanges();
                        }
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading document: {ex.Message}");
                    return View(driver);
                }
            }

            return View(driver);
        }

        // GET: Drivers/Edit/5
        public ActionResult Edit(int id)
        {
            var driver = db.Drivers
                .Include(d => d.User)
                .FirstOrDefault(d => d.DriverID == id);

            if (driver == null)
                return HttpNotFound();

            ViewBag.Documents = db.Documents
                .Where(d => d.EntityType == "Driver" && d.EntityID == id)
                .ToList();
            return View(driver);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Driver driver, HttpPostedFileBase licenseAttachment)
        {
            if (ModelState.IsValid)
            {
                var existingDriver = db.Drivers.Find(driver.DriverID);
                if (existingDriver == null)
                    return HttpNotFound();

                // Update driver fields
                //existingDriver.FirstName = driver.FirstName;
                //existingDriver.LastName = driver.LastName;
                existingDriver.LicenseNumber = driver.LicenseNumber;
                existingDriver.CreatedBy = Session["UserId"] != null ? (int?)Convert.ToInt32(Session["UserId"]) : null;
                existingDriver.UpdatedAt = DateTime.Now;

                // Handle file upload
                string docPath = Server.MapPath("~/UploadedFiles/InsuranceDocument/Driver/");
                if (!Directory.Exists(docPath))
                    Directory.CreateDirectory(docPath);

                try
                {
                    if (licenseAttachment != null && licenseAttachment.ContentLength > 0)
                    {
                        // Delete existing license attachment document, if any
                        var existingDocument = db.Documents
                            .FirstOrDefault(d => d.EntityType == "Driver" && d.EntityID == driver.DriverID && d.DocumentType == "LicenseAttachment");
                        if (existingDocument != null)
                        {
                            if (System.IO.File.Exists(existingDocument.FilePath))
                                System.IO.File.Delete(existingDocument.FilePath);
                            db.Documents.Remove(existingDocument);
                        }

                        // Save new file
                        string fileName = $"Driver{driver.DriverID}_LicenseAttachment_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(licenseAttachment.FileName)}";
                        string filePath = Path.Combine(docPath, fileName);
                        licenseAttachment.SaveAs(filePath);
                        //existingDriver.LicenseAttachment = filePath; // Optional: Set if used in model
                        db.Documents.Add(new Document
                        {
                            EntityType = "Driver",
                            EntityID = driver.DriverID,
                            DocumentType = "LicenseAttachment",
                            FilePath = filePath,
                            FileSize = licenseAttachment.ContentLength / 1024,
                            FileType = Path.GetExtension(licenseAttachment.FileName).TrimStart('.'),
                            UploadedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }

                    db.Entry(existingDriver).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating driver record or uploading document: {ex.Message}");
                }
            }

            ViewBag.Documents = db.Documents
                .Where(d => d.EntityType == "Driver" && d.EntityID == driver.DriverID)
                .ToList();
            return View(driver);
        }

        // GET: Drivers/Delete/5
        public ActionResult Delete(int id)
        {
            var driver = db.Drivers
                .Include(d => d.User)
                .FirstOrDefault(d => d.DriverID == id);

            if (driver == null)
                return HttpNotFound();

            ViewBag.Documents = db.Documents
                .Where(d => d.EntityType == "Driver" && d.EntityID == id)
                .ToList();
            return View(driver);
        }

        // POST: Drivers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var driver = db.Drivers
                .FirstOrDefault(d => d.DriverID == id);

            if (driver == null)
                return HttpNotFound();

            try
            {
                // Delete associated documents
                var documents = db.Documents
                    .Where(d => d.EntityType == "Driver" && d.EntityID == id)
                    .ToList();
                foreach (var document in documents)
                {
                    if (System.IO.File.Exists(document.FilePath))
                        System.IO.File.Delete(document.FilePath);
                    db.Documents.Remove(document);
                }

                db.Drivers.Remove(driver);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error deleting driver record or associated documents: {ex.Message}");
                ViewBag.Documents = db.Documents
                    .Where(d => d.EntityType == "Driver" && d.EntityID == id)
                    .ToList();
                return View(driver);
            }
        }

        // GET: Drivers/Details/5
        public ActionResult Details(int id)
        {
            var driver = db.Drivers
                .Include(d => d.User)
                .FirstOrDefault(d => d.DriverID == id);
            if (driver == null)
                return HttpNotFound();

            ViewBag.Documents = db.Documents
                .Where(d => d.EntityType == "Driver" && d.EntityID == id)
                .ToList();
            return View(driver);
        }

        // GET: Drivers/DownloadFile/5
        public ActionResult DownloadFile(int id)
        {
            var document = db.Documents.FirstOrDefault(d => d.DocumentID == id);
            if (document == null || !System.IO.File.Exists(document.FilePath))
                return HttpNotFound();

            var fileBytes = System.IO.File.ReadAllBytes(document.FilePath);
            var fileName = Path.GetFileName(document.FilePath);
            var contentType = MimeMapping.GetMimeMapping(fileName);
            return File(fileBytes, contentType, fileName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}