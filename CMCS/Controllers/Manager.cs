using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class ManagerController : Controller
    {
        private readonly FileEncryptionService _encryptionService;

        public ManagerController()
        {
            _encryptionService = new FileEncryptionService();
        }
        public IActionResult Dashboard()
        {
            var allClaims = ClaimDataStore.GetAllClaims();
            ViewBag.Claims = allClaims;

            ViewBag.PendingCount = ClaimDataStore.GetApprovalPendingCount();
            ViewBag.ApprovedCount = ClaimDataStore.GetApprovedCount();
            ViewBag.RejectedCount = ClaimDataStore.GetApprovalRejectedCount();

            return View();
        }

        [HttpGet]
        public IActionResult ClaimDetails(int claimId)
        {
            var claim = ClaimDataStore.GetClaimById(claimId);
            if (claim == null) return NotFound();
            ViewBag.Claim = claim;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(int claimId, string file)
        {
            var claim = ClaimDataStore.GetClaimById(claimId);
            if (claim == null || !claim.EncryptedDocuments.Contains(file))
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", $"claim-{claimId}", file);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            try
            {
                var memoryStream = await _encryptionService.DecryptFileAsync(filePath);
                var originalIndex = claim.EncryptedDocuments.IndexOf(file);
                var originalName = claim.OriginalDocuments[originalIndex];

                return File(memoryStream, "application/octet-stream", originalName);
            }
            catch
            {
                return BadRequest("Error decrypting the file.");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalizeClaim(int claimId, string status, string approvedBy)
        {
            if (string.IsNullOrWhiteSpace(approvedBy))
            {
                TempData["Error"] = "Approver name is required.";
                return RedirectToAction("Dashboard");
            }

            if (Enum.TryParse(status, true, out ClaimApprovalStatus newStatus) &&
                (newStatus == ClaimApprovalStatus.Approved || newStatus == ClaimApprovalStatus.Rejected))
            {
                ClaimDataStore.UpdateApprovalStatus(claimId, newStatus, approvedBy, "Manager");
                TempData["Message"] = $"Claim {claimId} {status.ToLower()} by {approvedBy}.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
