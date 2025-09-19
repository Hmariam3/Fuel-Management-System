using System;
using System.ComponentModel.DataAnnotations;
namespace FuelManagementSystem.Models
{
    public class ADUserViewModel
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string ADDepartment { get; set; }

        public int? Process { get; set; }
        public int? Subprocess { get; set; }
        public int? Branch { get; set; }
        public int? Department { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

}
