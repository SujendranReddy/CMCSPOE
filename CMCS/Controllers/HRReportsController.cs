using CMCS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

[Authorize(Roles = "HR")]
public class HRReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public HRReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult GeneratePdfReport(DateTime? startDate, DateTime? endDate, ClaimVerificationStatus? verificationStatus)
    {
        var claimsQuery = _context.Claims
                                  .Include(c => c.User)
                                  .AsQueryable();

        if (startDate.HasValue)
            claimsQuery = claimsQuery.Where(c => c.SubmittedOn >= startDate.Value);

        if (endDate.HasValue)
            claimsQuery = claimsQuery.Where(c => c.SubmittedOn <= endDate.Value);

        if (verificationStatus.HasValue)
            claimsQuery = claimsQuery.Where(c => c.VerificationStatus == verificationStatus.Value);

        var claimsList = claimsQuery.ToList();

        using var stream = new MemoryStream();
        var pdf = new PdfDocument();
        var page = pdf.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var fontHeader = new XFont("Arial", 12, XFontStyle.Bold);
        var fontRow = new XFont("Arial", 10, XFontStyle.Regular);

        int xStart = 40;
        int yPoint = 60;
        int rowHeight = 20;

        void DrawHeaders()
        {
            gfx.DrawString("ClaimID", fontHeader, XBrushes.Black, xStart, yPoint);
            gfx.DrawString("Lecturer", fontHeader, XBrushes.Black, xStart + 50, yPoint);
            gfx.DrawString("Month", fontHeader, XBrushes.Black, xStart + 200, yPoint);
            gfx.DrawString("Hours", fontHeader, XBrushes.Black, xStart + 250, yPoint);
            gfx.DrawString("Amount", fontHeader, XBrushes.Black, xStart + 300, yPoint);
            gfx.DrawString("Status", fontHeader, XBrushes.Black, xStart + 360, yPoint);
            gfx.DrawString("SubmittedOn", fontHeader, XBrushes.Black, xStart + 430, yPoint);

            yPoint += rowHeight;
        }

        gfx.DrawString("Claims Report", new XFont("Arial", 20, XFontStyle.Bold), XBrushes.Black, new XPoint(40, 30));

        DrawHeaders();

        foreach (var claim in claimsList)
        {
            string monthFormatted = claim.Month;
            var parts = claim.Month.Split(' ');
            if (parts.Length == 2 && int.TryParse(parts[1], out int year))
            {
                monthFormatted = $"{parts[0]} {year % 10000}";
            }

            var lecturerName = claim.User != null
                ? $"{claim.User.FirstName} {claim.User.LastName}"
                : "Unknown";

            gfx.DrawString(claim.ClaimID.ToString(), fontRow, XBrushes.Black, xStart, yPoint);
            gfx.DrawString(lecturerName, fontRow, XBrushes.Black, xStart + 50, yPoint);
            gfx.DrawString(monthFormatted, fontRow, XBrushes.Black, xStart + 200, yPoint); 
            gfx.DrawString(claim.HoursWorked.ToString(), fontRow, XBrushes.Black, xStart + 250, yPoint);
            gfx.DrawString(claim.TotalAmount.ToString("C"), fontRow, XBrushes.Black, xStart + 300, yPoint);
            gfx.DrawString(claim.VerificationStatus.ToString(), fontRow, XBrushes.Black, xStart + 360, yPoint);
            gfx.DrawString(claim.SubmittedOn.ToShortDateString(), fontRow, XBrushes.Black, xStart + 430, yPoint);

            yPoint += rowHeight;

            if (yPoint > page.Height - 50)
            {
                page = pdf.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPoint = 60;
                DrawHeaders();
            }
        }


        pdf.Save(stream, false);
        return File(stream.ToArray(), "application/pdf", "ClaimsReport.pdf");
    }
}
