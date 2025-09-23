using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using FuelManagementSystem.Models;

namespace FuelManagementSystem.Controllers
{
    public class PermissionsController : BaseController
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: Permissions
        //[PermissionAuthorize("View Permission")]
        public ActionResult Index()
        {
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];
            return View(db.Permissions.ToList());
        }

        // AJAX: Get Permission Details
        public JsonResult DetailsAjax(int id)
        {
            var permission = db.Permissions.Find(id);
            if (permission == null)
            {
                return Json(new { success = false, message = "Permission not found" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                success = true,
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description
            }, JsonRequestBehavior.AllowGet);
        }

        // AJAX: Create Permission
        [HttpPost]
        //[PermissionAuthorize("Create Permission")]
        public JsonResult CreateAjax([Bind(Include = "PermissionName,Description")] Permission permission)
        {
            try
            {
                if (permission == null)
                {
                    return Json(new { success = false, message = "Permission data is null" });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Validation errors: " + string.Join(", ", errors) });
                }
                if (db.Permissions.Any(p => p.PermissionName == permission.PermissionName))
                {
                    return Json(new { success = false, message = "Permission name already exists" });
                }
                db.Permissions.Add(permission);
                db.SaveChanges();
                return Json(new { success = true, message = "Permission created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        // AJAX: Edit Permission
        [HttpPost]
        //[PermissionAuthorize("Edit Permission")]
        public JsonResult EditAjax([Bind(Include = "PermissionId,PermissionName,Description")] Permission permission)
        {
            try
            {
                if (permission == null || permission.PermissionId <= 0)
                {
                    return Json(new { success = false, message = "Invalid permission ID" });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Validation errors: " + string.Join(", ", errors) });
                }
                var existing = db.Permissions.Find(permission.PermissionId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Permission not found" });
                }
                if (db.Permissions.Any(p => p.PermissionName == permission.PermissionName && p.PermissionId != permission.PermissionId))
                {
                    return Json(new { success = false, message = "Permission name already exists" });
                }
                existing.PermissionName = permission.PermissionName;
                existing.Description = permission.Description;
                db.Entry(existing).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, message = "Permission updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        // AJAX: Delete Permission
        [HttpPost]
        //[PermissionAuthorize("Delete Permission")]
        public JsonResult DeleteAjax(int id)
        {
            try
            {
                var permission = db.Permissions.Find(id);
                if (permission == null)
                {
                    return Json(new { success = false, message = "Permission not found" });
                }
                var rolePermissions = db.RolePermissions.Where(rp => rp.PermissionId == id).ToList();
                db.RolePermissions.RemoveRange(rolePermissions);
                db.Permissions.Remove(permission);
                db.SaveChanges();
                return Json(new { success = true, message = "Permission deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        // AJAX: Get All Roles
        public JsonResult GetAllRoles()
        {
            var roles = db.Roles.Select(r => new { r.role_id, r.role_name }).ToList();
            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        // AJAX: Get Role Permissions for a Permission
        public JsonResult GetRolePermissions(int permissionId)
        {
            var roleIds = db.RolePermissions
                .Where(rp => rp.PermissionId == permissionId)
                .Select(rp => rp.RoleId)
                .ToList();
            return Json(roleIds, JsonRequestBehavior.AllowGet);
        }

        // AJAX: Assign Permissions to Role
        [HttpPost]
        //[PermissionAuthorize("Assign to Role")]
        public JsonResult AssignPermissionsToRoleAjax(int permissionId, List<int> roleIds)
        {
            try
            {
                var permission = db.Permissions.FirstOrDefault(p => p.PermissionId == permissionId);
                if (permission == null)
                {
                    return Json(new { success = false, message = "Permission not found" });
                }
                if (roleIds != null && roleIds.Any())
                {
                    var validRoleIds = db.Roles.Select(r => r.role_id).ToList();
                    if (roleIds.Any(rid => !validRoleIds.Contains(rid)))
                    {
                        return Json(new { success = false, message = "One or more role IDs are invalid" });
                    }
                }
                var existingPermissions = db.RolePermissions.Where(rp => rp.PermissionId == permissionId).ToList();
                if (existingPermissions.Any())
                {
                    db.RolePermissions.RemoveRange(existingPermissions);
                }
                if (roleIds != null && roleIds.Any())
                {
                    foreach (var roleId in roleIds)
                    {
                        db.RolePermissions.Add(new RolePermission { PermissionId = permissionId, RoleId = roleId });
                    }
                }
                db.SaveChanges();
                return Json(new { success = true, message = "Permissions assigned successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        //public ActionResult SeedPermissions()
        //{
        //    var permissions = new List<Permission>
        //    {
        //        new Permission { PermissionName = "View Documents" },
        //        new Permission { PermissionName = "Edit Documents" },
        //        new Permission { PermissionName = "Delete Documents" },
        //        new Permission { PermissionName = "Submit Documents" },
        //        new Permission { PermissionName = "Manage Users" },
        //        new Permission { PermissionName = "Assign Roles" },
        //        new Permission { PermissionName = "Generate Reports" }
        //    };

        //    foreach (var perm in permissions)
        //    {
        //        if (!db.Permissions.Any(p => p.PermissionName == perm.PermissionName))
        //        {
        //            db.Permissions.Add(perm);
        //        }
        //    }
        //    db.SaveChanges();
        //    return Content("Permissions seeded successfully.");
        //}



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