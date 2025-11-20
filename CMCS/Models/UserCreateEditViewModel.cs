namespace CMCS.Models
{
    public class UserCreateEditViewModel
    {
        //ViewModel for HR
        public string? Id { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public decimal HourlyRate { get; set; }
        public int MaxHoursPerMonth { get; set; }
        public string? Password { get; set; } 
        public string Role { get; set; }
    }
}
