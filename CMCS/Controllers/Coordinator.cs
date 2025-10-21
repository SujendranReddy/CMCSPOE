using Microsoft.AspNetCore.Mvc;
using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using System.IO;
using System.Threading.Tasks;

namespace CMCS.Controllers
{
    public class CoordinatorController : Controller
    {
        private readonly FileEncryptionService _encryptionService;

        public CoordinatorController()
        {
            _encryptionService = new FileEncryptionService();
        }

        // Coordinator Dashboard
        public IActionResult Dashboard()
        {
            var claims = ClaimDataStore.GetAllClaims();
            ViewBag.Claims = claims;

            // Dashboard summary counts for verification
            ViewBag.VerificationPendingCount = ClaimDataStore.GetVerificationPendingCount();
            ViewBag.VerifiedCount = ClaimDataStore.GetVerifiedCount();
            ViewBag.VerificationRejectedCount = ClaimDataStore.GetVerificationRejectedCount();

            return View();
        }

        // View claim details
        [HttpGet]
        public IActionResult ClaimDetails(int claimId)
        {
            var claim = ClaimDataStore.GetClaimById(claimId);
            if (claim == null)
                return NotFound();

            ViewBag.Claim = claim;
            return View();
        }

        // Download decrypted document for viewing
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
        public IActionResult VerifyOrRejectClaim(int claimId, string verifiedBy, string actionType)
        {
            if (string.IsNullOrWhiteSpace(verifiedBy))
            {
                TempData["Error"] = "Name is required.";
                return RedirectToAction("Dashboard");
            }

            var claim = ClaimDataStore.GetClaimById(claimId);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            if (claim.VerificationStatus != ClaimVerificationStatus.Pending)
            {
                TempData["Error"] = "Only pending claims can be verified or rejected.";
                return RedirectToAction("Dashboard");
            }

            if (actionType == "verify")
            {
                ClaimDataStore.UpdateVerificationStatus(claimId, ClaimVerificationStatus.Verified, verifiedBy, "Coordinator");
                TempData["Message"] = $"Claim #{claimId} verified by {verifiedBy}.";
            }
            else if (actionType == "reject")
            {
                ClaimDataStore.UpdateVerificationStatus(claimId, ClaimVerificationStatus.Rejected, verifiedBy, "Coordinator");
                TempData["Message"] = $"Claim #{claimId} rejected by {verifiedBy}.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
