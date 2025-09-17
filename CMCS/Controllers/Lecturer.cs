using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class LecturerController : Controller
    {

        public IActionResult Dashboard()
        {
            var claims = new[]
            {
                new { Month = "July 2025", HoursWorked = 40, HourlyRate = 500, Status = "Approved" },
                new { Month = "August 2025", HoursWorked = 35, HourlyRate = 500, Status = "Pending" }
            };

            ViewBag.Claims = claims;
            return View();
        }

        public IActionResult SubmitClaim()
        {
            return View();
        }
        public IActionResult ClaimDetails(string month)
        {
            var claim = new
            {
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
