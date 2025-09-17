using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class CoordinatorController : Controller
    {
        // shows the coordinator dashboard
        // prepares a list of claims so the view can display them
        public IActionResult Dashboard()
        {
            // creates fake claims to simulate data that would normally come from the database
            var claims = new[]
            {
                new { Month = "July 2025", Lecturer = "John Doe", HoursWorked = 40, HourlyRate = 500, Status = "Pending" },
                new { Month = "August 2025", Lecturer = "Jane Smith", HoursWorked = 35, HourlyRate = 500, Status = "Pending" }
            };

            // adds the claims to the ViewBag so the view can access them easily
            ViewBag.Claims = claims;

            // returns the dashboard view
            return View();
        }

        // displays the details of a single claim for a given month
        public IActionResult ClaimDetails(string month)
        {
            // creates a fake claim to simulate what might be retrieved from the database
            var claim = new
            {
                ClaimID = 57,
                Month = month,
                HoursWorked = 40,
                HourlyRate = 500,
                Status = "Pending",
                SubmittedOn = "2025-07-05",
                Documents = new[] { "Invoice.pdf", "Report.docx" },
                ApprovedBy = "-" // Claim is still pending approval
            };

            // adds the claim to the ViewBag for the view to display
            ViewBag.Claim = claim;

            // returns the claim details view
            return View();
        }
    }
}
