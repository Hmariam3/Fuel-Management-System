using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FuelManagementSystem.Models;

public class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    private readonly string _permission;

    public PermissionAuthorizeAttribute(string permission)
    {
        _permission = permission;
    }

    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        var userIdObj = httpContext.Session["UserId"];
        if (userIdObj == null)
        {
            return false;
        }

        int userId = Convert.ToInt32(userIdObj);

        using (var db = new FuelManagementSystemEntities())
        {
            var user = db.Users
                        .Include("UserRoles.Role.RolePermissions.Permission")
                        .FirstOrDefault(u => u.UserId == userId);

            if (user?.UserRoles != null)
            {
                return user.UserRoles.Any(ur =>
                    ur.Role.RolePermissions.Any(rp =>
                        rp.Permission.PermissionName == _permission
                    )
                );
            }


            return false;
        }
    }

    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    {
        filterContext.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/Unauthorized.cshtml"
        };
    }
}
