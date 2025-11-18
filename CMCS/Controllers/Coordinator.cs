using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMCS.Controllers
{
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FileEncryptionService _encryptionService;

        public CoordinatorController(ApplicationDbContext context, FileEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var claims = await _context.Claims.ToListAsync();
            foreach (var claim in claims)
                claim.LoadDocumentLists();

            ViewBag.Claims = claims;
            ViewBag.VerificationPendingCount = claims.Count(c => c.VerificationStatus == ClaimVerificationStatus.Pending);
            ViewBag.VerifiedCount = claims.Count(c => c.VerificationStatus == ClaimVerificationStatus.Verified);
            ViewBag.VerificationRejectedCount = claims.Count(c => c.VerificationStatus == ClaimVerificationStatus.Rejected);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ClaimDetails(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null)
                return NotFound();

            claim.LoadDocumentLists();
            ViewBag.Claim = claim;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(int claimId, string file)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null)
                return NotFound();

            claim.LoadDocumentLists();

            if (!claim.EncryptedDocuments.Contains(file))
                return NotFound();

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                $"claim-{claimId}",
                file
            );

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
        public async Task<IActionResult> VerifyOrRejectClaim(int claimId, string verifiedBy, string actionType)
        {
            if (string.IsNullOrWhiteSpace(verifiedBy))
            {
                TempData["Error"] = "Name is required.";
                return RedirectToAction("Dashboard");
            }

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            claim.LoadDocumentLists();

            if (claim.VerificationStatus != ClaimVerificationStatus.Pending)
            {
                TempData["Error"] = "Only pending claims can be verified or rejected.";
                return RedirectToAction("Dashboard");
            }

            if (actionType == "verify")
            {
                claim.VerificationStatus = ClaimVerificationStatus.Verified;
                claim.VerifiedBy = verifiedBy;
                claim.VerifiedOn = DateTime.UtcNow;
                TempData["Message"] = $"Claim #{claimId} verified by {verifiedBy}.";
            }
            else if (actionType == "reject")
            {
                claim.VerificationStatus = ClaimVerificationStatus.Rejected;
                claim.VerifiedBy = verifiedBy;
                claim.VerifiedOn = DateTime.UtcNow;
                TempData["Message"] = $"Claim #{claimId} rejected by {verifiedBy}.";
            }

            claim.SaveDocumentLists();
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }
    }
}
