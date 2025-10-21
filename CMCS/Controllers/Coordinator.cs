using Microsoft.AspNetCore.Mvc;
using CMCS.Data;
using CMCS.Models;
using CMCS.Services;

namespace CMCS.Controllers
{
    public class CoordinatorController : Controller
    {
        private readonly FileEncryptionService _encryptionService;

        public CoordinatorController()
        {
            _encryptionService = new FileEncryptionService();
        }

        public IActionResult Dashboard()
        {
            var claims = ClaimDataStore.GetAllClaims();
            ViewBag.Claims = claims;

            ViewBag.VerificationPendingCount = ClaimDataStore.GetVerificationPendingCount();
            ViewBag.VerifiedCount = ClaimDataStore.GetVerifiedCount();
            ViewBag.VerificationRejectedCount = ClaimDataStore.GetVerificationRejectedCount();

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
                // This reads the encrypted file and returns a Memory stream which has the decrypted data
                var memoryStream = await _encryptionService.DecryptFileAsync(filePath);
                // The original file name corresponds to the encrypted file
                var originalIndex = claim.EncryptedDocuments.IndexOf(file);
                var originalName = claim.OriginalDocuments[originalIndex];

                return File(memoryStream, "application/octet-stream", originalName);
            }
            catch
            {
                // Just to catch any errors likes corrupted files or wrong keys
                return BadRequest("Error decrypting the file.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyClaim(int claimId, string verifiedBy)
        {
            // This Ensures a name is provided for tracking which coordinator verified the claim
            if (string.IsNullOrWhiteSpace(verifiedBy))
            {
                TempData["Error"] = "Verification name is required.";
                return RedirectToAction("Dashboard");
            }

            var claim = ClaimDataStore.GetClaimById(claimId);
            // This only allows verfication if the claim is pending
            if (claim != null && claim.VerificationStatus == ClaimVerificationStatus.Pending)
            {
                // This updates the claim's verification status and it stores who verified it
                ClaimDataStore.UpdateVerificationStatus(claimId, ClaimVerificationStatus.Verified, verifiedBy, "Coordinator");
                TempData["Message"] = $"Claim {claim.ClaimID} verified by {verifiedBy}.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
