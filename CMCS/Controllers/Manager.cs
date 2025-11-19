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
            var claims = await _context.Claims
                .Where(c => c.VerificationStatus == ClaimVerificationStatus.Verified)
                .Include(c => c.User)
                .ToListAsync();

            foreach (var claim in claims)
                claim.LoadDocumentLists();

            ViewBag.Claims = claims;
            ViewBag.PendingCount = claims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Pending);
            ViewBag.ApprovedCount = claims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Approved);
            ViewBag.RejectedCount = claims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Rejected);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ClaimDetails(int claimId)
        {
            var claim = await _context.Claims
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null || claim.VerificationStatus != ClaimVerificationStatus.Verified)
            {
                TempData["Error"] = "Claim not found or not verified.";
                return RedirectToAction("Dashboard");
            }

            claim.LoadDocumentLists();
            ViewBag.Claim = claim;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(int claimId, string file)
        {
            var claim = await _context.Claims
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null || claim.VerificationStatus != ClaimVerificationStatus.Verified)
            {
                TempData["Error"] = "Claim not found or not verified.";
                return RedirectToAction("Dashboard");
            }

            claim.LoadDocumentLists();

            if (claim.EncryptedDocuments == null || !claim.EncryptedDocuments.Contains(file))
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
        public async Task<IActionResult> FinalizeClaim(int claimId, string actionType)
        {
            var appUser = await _userManager.GetUserAsync(User);
            var managerName = appUser != null
                ? $"{appUser.FirstName} {appUser.LastName}".Trim()
                : User.Identity?.Name ?? "Manager";

            var claim = await _context.Claims
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null || claim.VerificationStatus != ClaimVerificationStatus.Verified)
            {
                TempData["Error"] = "This claim cannot be approved. It is not verified.";
                return RedirectToAction("Dashboard");
            }

            claim.LoadDocumentLists();

            if (string.Equals(actionType, "approve", StringComparison.OrdinalIgnoreCase))
            {
                claim.ApprovalStatus = ClaimApprovalStatus.Approved;
            }
            else if (string.Equals(actionType, "reject", StringComparison.OrdinalIgnoreCase))
            {
                claim.ApprovalStatus = ClaimApprovalStatus.Rejected;
            }
            else
            {
                TempData["Error"] = "Invalid action.";
                return RedirectToAction("Dashboard");
            }

            claim.ApprovedBy = managerName;
            claim.ApprovedOn = DateTime.UtcNow;

            claim.SaveDocumentLists();
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Claim #{claimId} {claim.ApprovalStatus.ToString().ToLower()} by {managerName}.";

            return RedirectToAction("Dashboard");
        }
    }
}
