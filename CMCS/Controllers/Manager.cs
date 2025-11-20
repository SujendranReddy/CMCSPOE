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
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FileEncryptionService _encryptionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerController(ApplicationDbContext context, FileEncryptionService encryptionService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _encryptionService = encryptionService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var verifiedClaims = await _context.Claims
                .Where(c => c.VerificationStatus == ClaimVerificationStatus.Verified)
                .ToListAsync();

            foreach (var claim in verifiedClaims)
                claim.LoadDocumentLists();

            ViewBag.Claims = verifiedClaims;
            ViewBag.PendingCount = verifiedClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Pending);
            ViewBag.ApprovedCount = verifiedClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Approved);
            ViewBag.RejectedCount = verifiedClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Rejected);

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
        public async Task<IActionResult> FinalizeClaim(int claimId, string status)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            claim.LoadDocumentLists();

            if (Enum.TryParse(status, true, out ClaimApprovalStatus newStatus) &&
                (newStatus == ClaimApprovalStatus.Approved || newStatus == ClaimApprovalStatus.Rejected))
            {
                var appUser = await _userManager.GetUserAsync(User);
                var managerName = appUser != null
                    ? $"{appUser.FirstName} {appUser.LastName}".Trim()
                    : User.Identity?.Name ?? "-";

                claim.ApprovalStatus = newStatus;
                claim.ApprovedBy = managerName;
                claim.ApprovedOn = DateTime.UtcNow;

                claim.SaveDocumentLists();
                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Claim #{claimId} {status.ToLower()} by {claim.ApprovedBy}.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
