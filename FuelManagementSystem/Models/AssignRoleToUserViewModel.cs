using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace FuelManagementSystem.ViewModels
{
    public class AssignRoleToUserViewModel
    {
        public int UserId { get; set; }
        public List<int> SelectedRoleIds { get; set; } // New list for multiple roles
        public List<SelectListItem> Users { get; set; }
        public List<SelectListItem> Roles { get; set; }
    }

}

