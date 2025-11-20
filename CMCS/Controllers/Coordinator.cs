using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMCS.Controllers
{
    // Only Coordinators  can access this controller
    [Authorize(Roles = "Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context; 
        private readonly FileEncryptionService _encryptionService; 
        private readonly UserManager<ApplicationUser> _userManager; 

        public CoordinatorController(ApplicationDbContext context, FileEncryptionService encryptionService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _encryptionService = encryptionService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            // This collects all the claims in the DB 
            var claims = await _context.Claims
                                       .Include(c => c.User)
                                       .ToListAsync();

            // This populates the doucments
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
            var claims = await _context.Claims
                                       .Include(c => c.User)
                                       .ToListAsync();

            // This finds the claim by ID
            var claim = await _context.Claims
                                      .Include(c => c.User)
                                      .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null)
                return NotFound(); // Returns error if claim is not found

            claim.LoadDocumentLists(); 
            ViewBag.Claim = claim; 
            return View();
        }

        // This allows coordinators to download the supporting documents
        [HttpGet]
        public async Task<IActionResult> DownloadFile(int claimId, string file)
        {
            var claim = await _context.Claims
                                      .Include(c => c.User)
                                      .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null)
                return NotFound();

            claim.LoadDocumentLists(); 

            // this checks if the file exists
            if (claim.EncryptedDocuments == null || !claim.EncryptedDocuments.Contains(file))
                return NotFound();

            // This builds the path 
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
                // Thi decrypts the file before sending to the coordinator
                var memoryStream = await _encryptionService.DecryptFileAsync(filePath);

                // This preserves the original file name 
                var originalIndex = claim.EncryptedDocuments.IndexOf(file);
                var originalName = claim.OriginalDocuments != null
                    ? claim.OriginalDocuments.ElementAtOrDefault(originalIndex) ?? file
                    : file;

                return File(memoryStream, "application/octet-stream", originalName);
            }
            catch
            {
                return BadRequest("Error decrypting the file.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> VerifyOrRejectClaim(int claimId, string actionType)
        {
            // This gets the current logged-in coordinator
            var appUser = await _userManager.GetUserAsync(User);

            // This builds the coordinator's name to record
            var verifier = appUser != null
                ? $"{appUser.FirstName} {appUser.LastName}".Trim()
                : User.Identity?.Name ?? "-";

            var claim = await _context.Claims
                                      .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            claim.LoadDocumentLists(); 

            // This ensures only pending claims can be verified/rejected
            if (claim.VerificationStatus != ClaimVerificationStatus.Pending)
            {
                TempData["Error"] = "Only pending claims can be verified or rejected.";
                return RedirectToAction("Dashboard");
            }

            if (string.Equals(actionType, "verify", StringComparison.OrdinalIgnoreCase))
            {
                claim.VerificationStatus = ClaimVerificationStatus.Verified;
                claim.VerifiedBy = verifier;
                claim.VerifiedOn = DateTime.UtcNow; 
                TempData["Message"] = $"Claim #{claimId} verified by {verifier}.";
            }
            else if (string.Equals(actionType, "reject", StringComparison.OrdinalIgnoreCase))
            {
                claim.VerificationStatus = ClaimVerificationStatus.Rejected;
                claim.VerifiedBy = verifier;
                claim.VerifiedOn = DateTime.UtcNow;
                TempData["Message"] = $"Claim #{claimId} rejected by {verifier}.";
            }

            claim.SaveDocumentLists();
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }
    }
}
