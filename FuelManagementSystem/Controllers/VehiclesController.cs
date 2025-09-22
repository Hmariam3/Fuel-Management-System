using System;
using System.Linq;
using System.Web.Mvc;
using FuelManagementSystem.Models;
using System.Data.Entity;

namespace FuelManagementSystem.Controllers
{
    public class VehiclesController : Controller
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: Vehicles
        public ActionResult Index()
        {
            var vehicles = db.Vehicles
                .Include(v => v.Branch)
                .Include(v => v.CostCenter)
                .Include(v => v.Driver)
                .Include(v => v.FuelType)
                .Include(v => v.UserOrgan1)
                .Include(v => v.VehicleAssignmentType1)
                .ToList();
            return View(vehicles);
        }

        // GET: Vehicles/Create
        public ActionResult Create()
        {
            var vehicle = new Vehicle
            {
                CreatedAt = DateTime.Now
            };
            ViewBag.BranchID = new SelectList(db.Branches, "BranchID", "BranchName");
            ViewBag.CostCenterID = new SelectList(db.CostCenters, "CostCenterID", "CostCenterName");
            ViewBag.VehicleAssignmentType = new SelectList(db.VehicleAssignmentTypes, "VehicleAssignmentTypeID", "TypeName");
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName");
            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName");
            ViewBag.UserOrgan = new SelectList(db.UserOrgans, "ID", "UserOrgan1");
            return View(vehicle);
        }

        // POST: Vehicles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                vehicle.CreatedAt = DateTime.Now;
                vehicle.UpdatedAt = DateTime.Now;
                vehicle.CreatedBy = Session["UserId"] != null ? (int?)Convert.ToInt32(Session["UserId"]) : null;

                db.Vehicles.Add(vehicle);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.BranchID = new SelectList(db.Branches, "BranchID", "BranchName", vehicle.BranchID);
            ViewBag.CostCenterID = new SelectList(db.CostCenters, "CostCenterID", "CostCenterName", vehicle.CostCenterID);
            ViewBag.VehicleAssignmentType = new SelectList(db.VehicleAssignmentTypes, "VehicleAssignmentTypeID", "TypeName", vehicle.VehicleAssignmentType);
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", vehicle.DriverID);
            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName", vehicle.FuelTypeID);
            ViewBag.UserOrgan = new SelectList(db.UserOrgans, "UserOrganID", "OrganName", vehicle.UserOrgan);
            return View(vehicle);
        }

        // GET: Vehicles/Edit/5
        public ActionResult Edit(string id)
        {
            var vehicle = db.Vehicles
                .Include(v => v.Branch)
                .Include(v => v.CostCenter)
                .Include(v => v.Driver)
                .Include(v => v.FuelType)
                .Include(v => v.UserOrgan1)
                .Include(v => v.VehicleAssignmentType1)
                .FirstOrDefault(v => v.PlateNo == id);

            if (vehicle == null)
                return HttpNotFound();

            ViewBag.BranchID = new SelectList(db.Branches, "BranchID", "BranchName", vehicle.BranchID);
            ViewBag.CostCenterID = new SelectList(db.CostCenters, "CostCenterID", "CostCenterName", vehicle.CostCenterID);
            ViewBag.VehicleAssignmentType = new SelectList(db.VehicleAssignmentTypes, "VehicleAssignmentTypeID", "TypeName", vehicle.VehicleAssignmentType);
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", vehicle.DriverID);
            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName", vehicle.FuelTypeID);
            ViewBag.UserOrgan = new SelectList(db.UserOrgans, "ID", "UserOrgan1", vehicle.UserOrgan);
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                var existingVehicle = db.Vehicles.Find(vehicle.PlateNo);
                if (existingVehicle == null)
                    return HttpNotFound();

                existingVehicle.PlateNo = vehicle.PlateNo;
                existingVehicle.VehicleType = vehicle.VehicleType;
                existingVehicle.ChassisNo = vehicle.ChassisNo;
                existingVehicle.MakeAndType = vehicle.MakeAndType;
                existingVehicle.DriverID = vehicle.DriverID;
                existingVehicle.FuelTypeID = vehicle.FuelTypeID;
                existingVehicle.UserOrgan = vehicle.UserOrgan;
                existingVehicle.CreatedBy = Session["UserId"] != null ? (int?)Convert.ToInt32(Session["UserId"]) : null;
                existingVehicle.UpdatedAt = DateTime.Now;
                existingVehicle.VehicleAssignmentType = vehicle.VehicleAssignmentType;
                existingVehicle.CostCenterID = vehicle.CostCenterID;
                existingVehicle.BranchID = vehicle.BranchID;

                db.Entry(existingVehicle).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating vehicle record: {ex.Message}");
                }
            }

            ViewBag.BranchID = new SelectList(db.Branches, "BranchID", "BranchName", vehicle.BranchID);
            ViewBag.CostCenterID = new SelectList(db.CostCenters, "CostCenterID", "CostCenterName", vehicle.CostCenterID);
            ViewBag.VehicleAssignmentType = new SelectList(db.VehicleAssignmentTypes, "VehicleAssignmentTypeID", "TypeName", vehicle.VehicleAssignmentType);
            ViewBag.DriverID = new SelectList(db.Drivers, "DriverID", "DriverName", vehicle.DriverID);
            ViewBag.FuelTypeID = new SelectList(db.FuelTypes, "FuelTypeID", "FuelTypeName", vehicle.FuelTypeID);
            ViewBag.UserOrgan = new SelectList(db.UserOrgans, "UserOrganID", "OrganName", vehicle.UserOrgan);
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        public ActionResult Delete(string id)
        {
            var vehicle = db.Vehicles
                .Include(v => v.Branch)
                .Include(v => v.CostCenter)
                .Include(v => v.Driver)
                .Include(v => v.FuelType)
                .Include(v => v.UserOrgan1)
                .Include(v => v.VehicleAssignmentType1)
                .Include(v => v.Ecards)
                .Include(v => v.FuelRefillRequests)
                .Include(v => v.FuelTransactions)
                .FirstOrDefault(v => v.PlateNo == id);

            if (vehicle == null)
                return HttpNotFound();

            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            var vehicle = db.Vehicles
                .Include(v => v.Ecards)
                .Include(v => v.FuelRefillRequests)
                .Include(v => v.FuelTransactions)
                .FirstOrDefault(v => v.PlateNo == id);

            if (vehicle == null)
                return HttpNotFound();

            if (vehicle.Ecards.Any() || vehicle.FuelRefillRequests.Any() || vehicle.FuelTransactions.Any())
            {
                ModelState.AddModelError("", "Cannot delete vehicle because it has related records (Ecards, Fuel Refill Requests, or Fuel Transactions).");
                return View(vehicle);
            }

            try
            {
                db.Vehicles.Remove(vehicle);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error deleting vehicle record: {ex.Message}");
                return View(vehicle);
            }
        }

        // GET: Vehicles/Details/5
        public ActionResult Details(string id)
        {
            var vehicle = db.Vehicles
                .Include(v => v.Branch)
                .Include(v => v.CostCenter)
                .Include(v => v.Driver)
                .Include(v => v.FuelType)
                .Include(v => v.UserOrgan1)
                .Include(v => v.VehicleAssignmentType1)
                .Include(v => v.Ecards)
                .Include(v => v.FuelRefillRequests)
                .Include(v => v.FuelTransactions)
                .FirstOrDefault(v => v.PlateNo == id);

            if (vehicle == null)
                return HttpNotFound();

            return View(vehicle);
        }

        // GET: Vehicles/GetActiveVehicles
        public ActionResult GetActiveVehicles()
        {
            try
            {
                var count = db.Vehicles.Count(v => v.VehicleAssignmentType.HasValue); // Assuming assigned vehicles are active
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
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}