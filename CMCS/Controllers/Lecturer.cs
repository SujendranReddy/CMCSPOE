using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMCS.Controllers
{
    // Only Lecturers can access this controller
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly FileEncryptionService _encryptionService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly long _maxFileSize = 5 * 1024 * 1024; 
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".xlsx" }; // allowed uploads

        public LecturerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = new FileEncryptionService(); // the file encryption helper
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); 

            // Get all claims for this lecturer
            var myClaims = await _context.Claims
                                         .Where(c => c.UserId == user.Id)
                                         .Include(c => c.User)
                                         .ToListAsync();

            foreach (var c in myClaims)
                c.LoadDocumentLists(); // this populates file lists

            ViewBag.Claims = myClaims;
            ViewBag.PendingCount = myClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Pending);
            ViewBag.ApprovedCount = myClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Approved);
            ViewBag.RejectedCount = myClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Rejected);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(int claimId, string file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null) return NotFound();

            if (claim.UserId != user.Id) return Forbid(); // This prevents seeing other lecturers claims

            claim.LoadDocumentLists();

            if (!claim.EncryptedDocuments.Contains(file))
                return NotFound();

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "uploads",
                $"claim-{claimId}",
                file
            );

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            try
            {
                var memoryStream = await _encryptionService.DecryptFileAsync(filePath);

                // This preserves original filename
                var originalIndex = claim.EncryptedDocuments.IndexOf(file);
                var originalName = claim.OriginalDocuments.ElementAtOrDefault(originalIndex) ?? file;

                return File(memoryStream, "application/octet-stream", originalName);
            }
            catch
            {
                return BadRequest("Error decrypting the file.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ClaimDetails(int claimId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var claim = await _context.Claims
                                      .Include(c => c.User)
                                      .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null) return NotFound();
            if (claim.UserId != user.Id) return Forbid();

            claim.LoadDocumentLists();
            ViewBag.Claim = claim;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SubmitClaim()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // This pre-fills claim with user rate and current month, however user can change the month
            var model = new Claim
            {
                HourlyRate = user.HourlyRate,
                Month = DateTime.UtcNow.ToString("MMMM yyyy")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim newClaim, List<IFormFile> uploadedFiles)
        {
            if (!User.Identity.IsAuthenticated) return Challenge();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // This removes any modelstate keys starting with "User" to prevent errors
            var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("User", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in keysToRemove)
                ModelState.Remove(k);

            if (!ModelState.IsValid) return View(newClaim);

            if (string.IsNullOrWhiteSpace(newClaim.Month))
                newClaim.Month = DateTime.UtcNow.ToString("MMMM yyyy");

            var month = newClaim.Month;

            // This calculates total hours worked in this month
            var monthlyHours = await _context.Claims
                .Where(c => c.UserId == user.Id && c.Month == month)
                .Select(c => (int?)c.HoursWorked)
                .SumAsync() ?? 0;

            var totalAfter = monthlyHours + newClaim.HoursWorked;

            // THis adjusts the hours if exceeding max allowed
            if (totalAfter > user.MaxHoursPerMonth)
            {
                int remainingAllowed = Math.Max(user.MaxHoursPerMonth - monthlyHours, 0);
                newClaim.HoursWorked = remainingAllowed;

                TempData["ErrorMessage"] =
                    $"You have exceeded your monthly limit. You will only be paid for the remaining hours allowed: {remainingAllowed}";
            }

            // This sets the claim properties
            newClaim.UserId = user.Id;
            newClaim.HourlyRate = user.HourlyRate;
            newClaim.SubmittedOn = DateTime.UtcNow;
            newClaim.VerificationStatus = ClaimVerificationStatus.Pending;
            newClaim.ApprovalStatus = ClaimApprovalStatus.Pending;

            uploadedFiles ??= new List<IFormFile>();

            // This saves the claim to the DB first to get ID
            _context.Claims.Add(newClaim);
            await _context.SaveChangesAsync();

            var claimFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", $"claim-{newClaim.ClaimID}");
            Directory.CreateDirectory(claimFolder);

            foreach (var file in uploadedFiles)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(ext)) continue;
                if (file.Length > _maxFileSize) continue;

                var encryptedName = $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}{ext}.enc";
                var filePath = Path.Combine(claimFolder, encryptedName);

                using var stream = file.OpenReadStream();
                await _encryptionService.EncryptFileAsync(stream, filePath);

                newClaim.EncryptedDocuments.Add(encryptedName);
                newClaim.OriginalDocuments.Add(file.FileName);
            }

            newClaim.SaveDocumentLists();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("Dashboard");
        }
    }
}
