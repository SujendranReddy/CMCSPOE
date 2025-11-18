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
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FileEncryptionService _encryptionService;

        public ManagerController(ApplicationDbContext context, FileEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var allClaims = await _context.Claims.ToListAsync();
            foreach (var claim in allClaims)
                claim.LoadDocumentLists();

            ViewBag.Claims = allClaims;
            ViewBag.PendingCount = allClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Pending);
            ViewBag.ApprovedCount = allClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Approved);
            ViewBag.RejectedCount = allClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Rejected);

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
        public async Task<IActionResult> FinalizeClaim(int claimId, string status, string approvedBy)
        {
            if (string.IsNullOrWhiteSpace(approvedBy))
            {
                TempData["Error"] = "Approver name is required.";
                return RedirectToAction("Dashboard");
            }

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
                claim.ApprovalStatus = newStatus;
                claim.ApprovedBy = approvedBy;
                claim.ApprovedOn = DateTime.UtcNow;

                claim.SaveDocumentLists();
                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Claim #{claimId} {status.ToLower()} by {approvedBy}.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
