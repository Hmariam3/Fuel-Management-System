using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FuelManagementSystem.ADautho;
using FuelManagementSystem.Models;

namespace FuelManagementSystem.Controllers
{
    public class UsersController : BaseController
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: Users
        //[PermissionAuthorize("View User")]
        public ActionResult Index()
        {
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.Process = Session["Process"];
            ViewBag.email = Session["email"];
            ViewBag.Processes = db.Processes.ToList();
            return View(db.Users.ToList());
        }

        // GET: Users/Details/5
        //public ActionResult Details(int? id)
        //{
        //    ViewBag.User = Session["username"];
        //    ViewBag.Fullname = Session["Fullname"];
        //    ViewBag.email = Session["email"];
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    User user = db.Users.Find(id);
        //    if (user == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(user);
        //}

        public JsonResult Details(int? id)
        {
            if (id == null)
                return Json(new { success = false, message = "Invalid ID" }, JsonRequestBehavior.AllowGet);

            User user = db.Users.Find(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" }, JsonRequestBehavior.AllowGet);

            // Create a flat JSON object to avoid serializing navigation properties
            var userData = new
            {
                user.UserId,
                user.ADUsername,
                user.FullName,
                user.Email,
                user.Position,
                Process = user.Process, // Assuming Process is an int? (ID)
                Subprocess = user.Subprocess, // Assuming Subprocess is an int? (ID)
                Branch = user.Branch, // Assuming Branch is an int? (ID)
                Department = user.Department, // Assuming Department is an int? (ID)
                                              // Optionally include names for display
                ProcessName = user.Process.HasValue ? db.Processes.Where(p => p.proces_Id == user.Process).Select(p => p.process_name).FirstOrDefault() : null,
                SubprocessName = user.Subprocess.HasValue ? db.Subprocesses.Where(s => s.subprocess_Id == user.Subprocess).Select(s => s.subprocess_name).FirstOrDefault() : null,
                BranchName = user.Branch.HasValue ? db.Branches.Where(b => b.ID == user.Branch).Select(b => b.BranchName).FirstOrDefault() : null,
                DepartmentName = user.Department.HasValue ? db.Departments.Where(d => d.department_Id == user.Department).Select(d => d.department_Name).FirstOrDefault() : null
            };

            return Json(userData, JsonRequestBehavior.AllowGet);
        }

        // GET: Users/Create
        //[PermissionAuthorize("Create User")]
        public ActionResult Create()
        {
            var processes = db.Processes.ToList(); // or wherever you're fetching from
            ViewBag.Processes = processes;
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];

            return View();
        }


        [HttpPost]
        public JsonResult SaveUserFromAD(ADUserViewModel model)
        {
            if (!db.Users.Any(u => u.ADUsername == model.Username))
            {
                var CreatedBy = Session["username"].ToString();
                db.Users.Add(new User
                {
                    ADUsername = model.Username,
                    FullName = model.FullName,
                    Email = model.Email,
                    Position = model.Position,
                    Process = model.Process,
                    Subprocess = model.Subprocess,
                    Branch = model.Branch,
                    Department = model.Department,
                    CreatedBy = CreatedBy,
                    CreatedDate = model.CreatedDate

                });

                db.SaveChanges();
                return Json(new { success = true, message = "User saved successfully!" });
            }

            return Json(new { success = false, message = "User already exists." });
        }

        public JsonResult GetSubprocessesByProcessId(int processId)
        {
            var subprocesses = db.Subprocesses.Where(s => s.process_Id == processId).Select(s => new { s.subprocess_Id, s.subprocess_name }).ToList();
            return Json(subprocesses, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBranchesBySubprocessId(string subprocess)
        {
            var branches = db.Branches.Where(b => b.SubProcess == subprocess).Select(b => new { b.ID, b.BranchName }).ToList();
            return Json(branches, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDepartmentsBySubprocessId(int subprocessId)
        {
            var department = db.Departments.Where(b => b.subprocess_id == subprocessId).Select(b => new { b.department_Id, b.department_Name }).ToList();
            return Json(department, JsonRequestBehavior.AllowGet);
        }

        // GET: Get Groups
        public ActionResult AssignRole_Permission()
        {
            var authHelper = new ActiveDirectoryHelper();
            var adGroups = authHelper.GetAllGroupsFromAD();
            var adTeams = authHelper.GetAllTeamsFromAD();


            ViewBag.adTeams = new SelectList(adTeams);

            ViewBag.ADGroups = new SelectList(adGroups);


            // If you have roles in DB
            ViewBag.Roles = new SelectList(db.Roles.ToList(), "role_id", "role_name");

            return View();
        }


        [HttpGet]
        public JsonResult SearchADUsers(string keyword)
        {
            var helper = new ActiveDirectoryHelper();
            var result = helper.SearchADUsers(keyword); // filters by full name inside the helper
            return Json(result, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public JsonResult GetUsersByGroup(string groupName)
        {
            var helper = new ActiveDirectoryHelper();
            var usernames = helper.GetUserListInDepartment(groupName);

            var userList = new List<ADUserViewModel>();

            foreach (var uname in usernames)
            {
                string fullname = helper.GetUserFullName(uname);
                userList.Add(new ADUserViewModel
                {
                    Username = uname,
                    FullName = fullname
                });
            }

            return Json(userList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUserByUsername(string username)
        {
            var helper = new ActiveDirectoryHelper();
            var user = helper.GetUserByUsername(username);
            return Json(user, JsonRequestBehavior.AllowGet);
        }




        //[HttpPost]
        //public ActionResult AssignGroupRole(string GroupName, int RoleId)
        //{
        //    if (!string.IsNullOrEmpty(GroupName))
        //    {
        //        var existing = db.Roles.FirstOrDefault(g => g.role_name == GroupName);
        //        if (existing != null)
        //        {
        //            existing.role_id = RoleId; // update if needed
        //        }
        //        else
        //        {
        //            db.Roles.Add(new Roles
        //            {
        //                role_name = GroupName,
        //                RoleId = RoleId
        //            });
        //        }

        //        db.SaveChanges();
        //        TempData["Success"] = "Role assigned to group successfully.";
        //    }

        //    return RedirectToAction("AssignGroupRole");
        //}

        // GET: Users/Edit/5

        public ActionResult Edit(int? id)
        {
            var processes = db.Processes.ToList(); // or wherever you're fetching from
            ViewBag.Processes = processes;
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "UserId,ADUsername,FullName,Email,Role,IsActive,CreatedAt")] User user)
        //{

        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(user).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    return View(user);
        //}

        [HttpPost]
        //[PermissionAuthorize("Edit User")]
        public JsonResult Edit(User user)
        {
            if (ModelState.IsValid)
            {
                // Fetch the original user to get CreatedBy and CreatedDate
                var originalUser = db.Users.AsNoTracking().FirstOrDefault(u => u.UserId == user.UserId);
                if (originalUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Attach the user and mark as modified
                db.Entry(user).State = EntityState.Modified;

                // Preserve original CreatedBy and CreatedDate
                db.Entry(user).Property(u => u.CreatedBy).CurrentValue = originalUser.CreatedBy;
                db.Entry(user).Property(u => u.CreatedDate).CurrentValue = originalUser.CreatedDate;

                db.SaveChanges();
                return Json(new { success = true, message = "User updated successfully" });
            }
            return Json(new { success = false, message = "Invalid data" });
        }

        // GET: Users/Delete/5

        public ActionResult Delete(int? id)
        {
            ViewBag.User = Session["username"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.email = Session["email"];
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    User user = db.Users.Find(id);
        //    db.Users.Remove(user);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

        [HttpPost]
        //[PermissionAuthorize("Delete User")]
        public JsonResult Delete(int id)
        {
            User user = db.Users.Find(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });
            db.Users.Remove(user);
            db.SaveChanges();
            return Json(new { success = true, message = "User deleted successfully" });
        }

        //[PermissionAuthorize("View User")]
        public JsonResult GetUserCount()
        {
            var count = db.Users.Count(u => u.CreatedBy == "hailemariamk"); // Adjust as per your model
            return Json(new { count }, JsonRequestBehavior.AllowGet);
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
