using Microsoft.AspNetCore.Identity;
using CMCS;

namespace CMCS
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public decimal HourlyRate { get; set; }
        public int MaxHoursPerMonth { get; set; }
    }
}