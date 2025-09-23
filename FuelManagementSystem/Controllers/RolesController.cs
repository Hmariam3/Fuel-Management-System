using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FuelManagementSystem.Models;
using FuelManagementSystem.ViewModels;

namespace FuelManagementSystem.Controllers
{
    public class RolesController : BaseController
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: Roles
        //[PermissionAuthorize("View Role")]
        public ActionResult Index()
        {
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];
            return View(db.Roles.ToList());
        }

        // GET: Roles/Details/5
        [HttpGet]
        public JsonResult Details(int? id)
        {
            if (id == null)
                return Json(new { success = false, message = "Invalid role ID" }, JsonRequestBehavior.AllowGet);

            Role role = db.Roles.Find(id);
            if (role == null)
                return Json(new { success = false, message = "Role not found" }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                role_id = role.role_id,
                role_name = role.role_name,
                description = role.description
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Create()
        {
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];
            return View();
        }

        // POST: Roles/Create
        [HttpPost]
        //[PermissionAuthorize("Create Role")]
        [ValidateAntiForgeryToken]
        public JsonResult Create([Bind(Include = "role_name,description")] Role role)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (db.Roles.Any(r => r.role_name.Trim().ToLower() == role.role_name.Trim().ToLower()))
                    {
                        return Json(new { success = false, message = "A role with this name already exists." });
                    }

                    db.Roles.Add(role);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Role created", roleId = role.role_id });
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join("; ", errors) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        public ActionResult Edit(int? id)
        {
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Role role = db.Roles.Find(id);
            if (role == null)
                return HttpNotFound();

            return View(role);
        }

        // POST: Roles/Edit
        [HttpPost]
        //[PermissionAuthorize("Edit Role")]
        [ValidateAntiForgeryToken]
        public JsonResult Edit([Bind(Include = "role_id,role_name,description")] Role role)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingRole = db.Roles.Find(role.role_id);
                    if (existingRole == null)
                        return Json(new { success = false, message = "Role not found" });

                    // Check for duplicate role name (excluding current role)
                    if (db.Roles.Any(r => r.role_name.Trim().ToLower() == role.role_name.Trim().ToLower() && r.role_id != role.role_id))
                    {
                        return Json(new { success = false, message = "A role with this name already exists." });
                    }

                    existingRole.role_name = role.role_name;
                    existingRole.description = role.description;
                    db.Entry(existingRole).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Role updated successfully" });
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join("; ", errors) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Role role = db.Roles.Find(id);
            if (role == null)
                return HttpNotFound();

            return View(role);
        }

        // POST: Roles/Delete/5
        [HttpPost]
        //[PermissionAuthorize("Delete Role")]
        //[ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            try
            {
                Role role = db.Roles.Find(id);
                if (role == null)
                    return Json(new { success = false, message = "Role not found" });

                db.Roles.Remove(role);
                db.SaveChanges();
                return Json(new { success = true, message = "Role deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpGet]
        //[PermissionAuthorize("Assign Role")]
        public ActionResult AssignRoleToUser()
        {
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];
            var viewModel = new AssignRoleToUserViewModel
            {
                Users = db.Users
                          .Select(u => new SelectListItem
                          {
                              Value = u.UserId.ToString(),
                              Text = u.FullName + " (" + u.ADUsername + ")"
                          }).ToList(),

                Roles = db.Roles
                          .Select(r => new SelectListItem
                          {
                              Value = r.role_id.ToString(),
                              Text = r.role_name
                          }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        //[PermissionAuthorize("Assign Role")]
        [ValidateAntiForgeryToken]
        public ActionResult AssignRoleToUser(AssignRoleToUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.Include("UserRoles").FirstOrDefault(u => u.UserId == model.UserId);
                if (user != null)
                {
                    // Clear existing roles
                    user.UserRoles.Clear();

                    // Assign selected roles
                    if (model.SelectedRoleIds != null)
                    {
                        foreach (var roleId in model.SelectedRoleIds)
                        {
                            user.UserRoles.Add(new UserRole { UserID = model.UserId, RoleID = roleId });
                        }
                    }

                    db.SaveChanges();

                    TempData["Success"] = "Roles assigned successfully!";
                    return RedirectToAction("AssignRoleToUser");
                }

                ModelState.AddModelError("", "User not found.");
            }

            // Reload dropdowns
            model.Users = db.Users.Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = u.FullName + " (" + u.ADUsername + ")"
            }).ToList();

            model.Roles = db.Roles.Select(r => new SelectListItem
            {
                Value = r.role_id.ToString(),
                Text = r.role_name
            }).ToList();

            return View(model);
        }


        [HttpPost]
        //[PermissionAuthorize("Assign Role")]
        [ValidateAntiForgeryToken]
        public JsonResult AssignRolesToUserAjax(int userId, List<int> roleIds)
        {
            try
            {
                var user = db.Users.FirstOrDefault(u => u.UserId == userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Explicitly remove existing roles from UserRoles table
                var existingRoles = db.UserRoles.Where(ur => ur.UserID == userId).ToList();
                if (existingRoles.Any())
                {
                    db.UserRoles.RemoveRange(existingRoles);
                }

                // Add new roles if any are provided
                if (roleIds != null && roleIds.Any())
                {
                    foreach (var roleId in roleIds)
                    {
                        db.UserRoles.Add(new UserRole { UserID = userId, RoleID = roleId });
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Roles updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating roles." });
            }
        }


        public JsonResult GetAllRoles()
        {
            var roles = db.Roles.Select(r => new { r.role_id, r.role_name }).ToList();
            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAllPermissions()
        {
            var permissions = db.Permissions.Select(p => new { p.PermissionId, p.PermissionName }).ToList();
            return Json(permissions, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPermissionsByRole(int roleId)
        {
            var assignedPermissions = db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToList();

            return Json(assignedPermissions, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //[PermissionAuthorize("Update RolePermission")]
        public JsonResult UpdateRolePermissions(int roleId, List<int> permissionIds)
        {
            var existing = db.RolePermissions.Where(rp => rp.RoleId == roleId);
            db.RolePermissions.RemoveRange(existing);

            if (permissionIds != null && permissionIds.Any())
            {
                foreach (var pid in permissionIds)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });
                }
            }
            db.SaveChanges();
            return Json(new { success = true });
        }

        public JsonResult GetAllUsers()
        {
            var users = db.Users
                .Select(u => new {
                    UserId = u.UserId,
                    FullName = u.FullName
                })
                .ToList();

            return Json(users, JsonRequestBehavior.AllowGet);
        }


        public JsonResult GetUserRoles(int userId)
        {
            var roleIds = db.UserRoles
                            .Where(ur => ur.UserID == userId)
                            .Select(ur => ur.RoleID)
                            .ToList();

            return Json(roleIds, JsonRequestBehavior.AllowGet);
        }


        public bool UserHasPermission(string username, string permissionName)
        {
            var user = db.Users.Include("UserRoles").FirstOrDefault(u => u.ADUsername == username);
            if (user == null) return false;

            var roleIds = user.UserRoles.Select(ur => ur.RoleID).ToList();

            var permissions = db.RolePermissions
                                .Where(rp => roleIds.Contains(rp.RoleId))
                                .Select(rp => rp.Permission.PermissionName)
                                .ToList();

            return permissions.Contains(permissionName);
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
