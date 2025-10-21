using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CMCS.Controllers
{
    public class LecturerController : Controller
    {
        private readonly FileEncryptionService _encryptionService;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".xlsx" };

        public LecturerController()
        {
            _encryptionService = new FileEncryptionService();
        }

        public IActionResult Dashboard()
        {
            var allClaims = ClaimDataStore.GetAllClaims();

            ViewBag.Claims = allClaims;
            ViewBag.PendingCount = allClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Pending);
            ViewBag.ApprovedCount = allClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Approved);
            ViewBag.RejectedCount = allClaims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Rejected);

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

        [HttpGet]
        public IActionResult ClaimDetails(int claimId)
        {
            var claim = ClaimDataStore.GetClaimById(claimId);
            if (claim == null) return NotFound();

            ViewBag.Claim = claim;
            return View();
        }

        [HttpGet]
        public IActionResult SubmitClaim()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim newClaim, List<IFormFile> uploadedFiles)
        {
            if (!ModelState.IsValid)
                return View(newClaim);

            newClaim.SubmittedOn = DateTime.UtcNow;

            newClaim.VerificationStatus = ClaimVerificationStatus.Pending;
            newClaim.ApprovalStatus = ClaimApprovalStatus.Pending;

            uploadedFiles ??= new List<IFormFile>();

            ClaimDataStore.AddClaim(newClaim);

            var claimFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", $"claim-{newClaim.ClaimID}");
            Directory.CreateDirectory(claimFolder);

            foreach (var file in uploadedFiles)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!_allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("", $"File type {ext} not allowed.");
                    return View(newClaim);
                }

                if (file.Length > _maxFileSize)
                {
                    ModelState.AddModelError("", $"File {file.FileName} exceeds {_maxFileSize / (1024 * 1024)} MB limit.");
                    return View(newClaim);
                }

                var encryptedName = $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}{ext}.enc";
                var filePath = Path.Combine(claimFolder, encryptedName);

                try
                {
                    using var stream = file.OpenReadStream();
                    await _encryptionService.EncryptFileAsync(stream, filePath);

                    newClaim.EncryptedDocuments.Add(encryptedName);
                    newClaim.OriginalDocuments.Add(file.FileName);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to encrypt file {file.FileName}: {ex.Message}");
                    return View(newClaim);
                }
            }

            if (newClaim.EncryptedDocuments.Any())
                ClaimDataStore.AppendEncryptedDocuments(newClaim.ClaimID, newClaim.EncryptedDocuments);

            if (newClaim.OriginalDocuments.Any())
                ClaimDataStore.AppendOriginalDocuments(newClaim.ClaimID, newClaim.OriginalDocuments);

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("Dashboard");
        }
    }
}
