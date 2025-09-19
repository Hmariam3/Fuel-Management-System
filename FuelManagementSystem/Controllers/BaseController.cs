using System;
using System.Web;
using System.Web.Mvc;

namespace FuelManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // 🔐 Redirect to Login if session is missing
            if (Session["UserId"] == null)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
                return; // Stop further execution
            }

            // 🚀 Populate ViewBag with session data for all controllers/views
            ViewBag.UserId = Session["UserId"];
            ViewBag.Fullname = Session["Fullname"];
            ViewBag.Username = Session["username"];
            ViewBag.Email = Session["email"];
            ViewBag.Role = Session["Role"];
            ViewBag.Process = Session["Process"];
            if (Session["memberSince"] != null)
            {
                var memberSince = Convert.ToDateTime(Session["memberSince"]);
                ViewBag.MemberSince = memberSince.ToString("MMM d, yyyy"); // Aug 5, 2025
            }

            // 🚫 Prevent browser from caching secured pages
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetValidUntilExpires(false);
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            base.OnActionExecuting(filterContext);
        }
    }
}
