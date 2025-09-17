using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class Manager : Controller
    {
        public IActionResult Dashboard()
        {
            // Identical to Coordinator controller
            var claims = new[]
            {
                new { Month = "July 2025", Lecturer = "John Doe", HoursWorked = 40, HourlyRate = 500, Status = "Pending" },
                new { Month = "August 2025", Lecturer = "Jane Smith", HoursWorked = 35, HourlyRate = 500, Status = "Pending" }
            };

            ViewBag.Claims = claims;
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
                Status = "Pending",
                SubmittedOn = "2025-07-05",
                Documents = new[] { "Invoice.pdf", "Report.docx" },
                ApprovedBy = "-"

            };

            ViewBag.Claim = claim;
            return View();
        }
    }
}