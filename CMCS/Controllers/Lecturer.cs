using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class LecturerController : Controller
    {
        //Idententical to other controllers
        public IActionResult Dashboard()
        {
            var claims = new[]
            {
                new { Month = "July 2025", HoursWorked = 40, HourlyRate = 500, Status = "Pending" },
                new { Month = "August 2025", HoursWorked = 35, HourlyRate = 500, Status = "Pending" }
            };

            ViewBag.Claims = claims;
            return View();
        }
        // This is for directing the user to submitclaim page when they click the button 
        public IActionResult SubmitClaim()
        {
            return View();
        }
        public IActionResult ClaimDetails(string month)
        {
            var claim = new
            {
                ClaimID = 57,
                Month = month,
                HoursWorked = 40,
                HourlyRate = 500,
                TotalAmount = 20000,
                Status = "Pending",
                SubmittedOn = "2025-07-05",
                SubmittedBy = "John Doe",
                Documents = new[] { "Invoice.pdf", "Report.docx" },
                ApprovedBy = "-" 
            };
            ViewBag.Claim = claim;
            return View();
        }

    }
}
