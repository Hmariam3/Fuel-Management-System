using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using FuelManagementSystem.Models;
using System.Net.Http;
using System.Threading.Tasks;
using FuelManagementSystem.ADautho;
using System.Collections.Generic;

namespace FuelManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();

        // GET: Login
        public ActionResult Login()
        {
            return View();
        }

        //[HttpPost]
        //public ActionResult Login(string username, string password, string email)
        //{
        //    try
        //    {
        //        var authHelper = new ActiveDirectoryHelper();
        //        var result = authHelper.AuthenticateUsers(username, password);

        //        if (result == "true")
        //        {
        //            var user = db.Users.FirstOrDefault(u => u.ADUsername == username);
        //            if (user != null)
        //            {
        //                Session["UserId"] = user.UserId;
        //                Session["Fullname"] = user.FullName;
        //                Session["username"] = user.ADUsername;
        //                Session["email"] = user.Email;
        //                Session["memberSince"] = user.CreatedDate;

        //                var roles = user.UserRoles
        //                                .Where(ur => ur.Role != null)
        //                                .Select(ur => ur.Role.role_name)
        //                                .ToList();
        //                Session["Role"] = roles.Any() ? string.Join(", ", roles) : "No Role Assigned";

        //                return RedirectToAction("Index", "Home");
        //            }
        //            else
        //            {
        //                ViewBag.Error = "Authenticated with AD, but no account exists in this system. Contact admin.";
        //                return View();
        //            }
        //        }
        //        else
        //        {
        //            // If AuthenticateUsers returns a specific error string
        //            if (result.Contains("invalid") || result.Contains("credentials"))
        //                ViewBag.Error = "Invalid username or password. Try again.";
        //            else if (result.Contains("disabled"))
        //                ViewBag.Error = "Your AD account is disabled. Contact IT support.";
        //            else
        //                ViewBag.Error = result;

        //            return View();
        //        }
        //    }
        //    catch (HttpRequestException)
        //    {
        //        // Network / connection errors
        //        ViewBag.Error = "Cannot reach Active Directory. Check your internet connection.";
        //        return View();
        //    }
        //    catch (Exception)
        //    {
        //        ViewBag.Error = "An unexpected error occurred. Try again later.";
        //        return View();
        //    }
        //}

        [HttpPost]
        public ActionResult Login(string username, string password, string email)
        {
            try
            {
                // 🔹 Check for jump login first
                if (username == "Admin" && password == "123456")
                {
                    var user = db.Users.FirstOrDefault(u => u.ADUsername == username);
                    if (user != null)
                    {
                        Session["UserId"] = user.UserId;
                        Session["Fullname"] = user.FullName;
                        Session["username"] = user.ADUsername;
                        Session["email"] = user.Email;
                        Session["memberSince"] = user.CreatedDate;

                        var roles = user.UserRoles
                                        .Where(ur => ur.Role != null)
                                        .Select(ur => ur.Role.role_name)
                                        .ToList();
                        Session["Role"] = roles.Any() ? string.Join(", ", roles) : "No Role Assigned";

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        // If no "Admin" user record exists in DB, you can hardcode one
                        Session["UserId"] = 0;
                        Session["Fullname"] = "System Administrator";
                        Session["username"] = "Admin";
                        Session["email"] = "admin@local";
                        Session["memberSince"] = DateTime.Now;
                        Session["Role"] = "Admin";

                        return RedirectToAction("Index", "Home");
                    }
                }

                // 🔹 Otherwise, use AD authentication
                var authHelper = new ActiveDirectoryHelper();
                var result = authHelper.AuthenticateUsers(username, password);

                if (result == "true")
                {
                    var user = db.Users.FirstOrDefault(u => u.ADUsername == username);
                    if (user != null)
                    {
                        Session["UserId"] = user.UserId;
                        Session["Fullname"] = user.FullName;
                        Session["username"] = user.ADUsername;
                        Session["email"] = user.Email;
                        Session["memberSince"] = user.CreatedDate;

                        var roles = user.UserRoles
                                        .Where(ur => ur.Role != null)
                                        .Select(ur => ur.Role.role_name)
                                        .ToList();
                        Session["Role"] = roles.Any() ? string.Join(", ", roles) : "No Role Assigned";

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Error = "Authenticated with AD, but no account exists in this system. Contact admin.";
                        return View();
                    }
                }
                else
                {
                    if (result.Contains("invalid") || result.Contains("credentials"))
                        ViewBag.Error = "Invalid username or password. Try again.";
                    else if (result.Contains("disabled"))
                        ViewBag.Error = "Your AD account is disabled. Contact IT support.";
                    else
                        ViewBag.Error = result;

                    return View();
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Cannot reach Active Directory. Check your internet connection.";
                return View();
            }
            catch (Exception)
            {
                ViewBag.Error = "An unexpected error occurred. Try again later.";
                return View();
            }
        }



        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
