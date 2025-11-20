using CMCS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

// Only HR can access this controller
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

        // This filters claims by start date if provided
        if (startDate.HasValue)
            claimsQuery = claimsQuery.Where(c => c.SubmittedOn >= startDate.Value);

        // This filters claims by end date if provided
        if (endDate.HasValue)
            claimsQuery = claimsQuery.Where(c => c.SubmittedOn <= endDate.Value);

        // This filters claims by verification status if selected
        if (verificationStatus.HasValue)
            claimsQuery = claimsQuery.Where(c => c.VerificationStatus == verificationStatus.Value);

        var claimsList = claimsQuery.ToList();

        // This prepares the PDF
        using var stream = new MemoryStream();
        var pdf = new PdfDocument();
        var page = pdf.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        // This sets up fonts for header and rows
        var fontHeader = new XFont("Arial", 12, XFontStyle.Bold);
        var fontRow = new XFont("Arial", 10, XFontStyle.Regular);

        int xStart = 40; // This is the left margin
        int yPoint = 60; // This is the starting vertical point
        int rowHeight = 20; // This is the height for each row

        void DrawHeaders()
        {
            gfx.DrawString("ClaimID", fontHeader, XBrushes.Black, xStart, yPoint);
            gfx.DrawString("Lecturer", fontHeader, XBrushes.Black, xStart + 50, yPoint);
            gfx.DrawString("Month", fontHeader, XBrushes.Black, xStart + 200, yPoint);
            gfx.DrawString("Hours", fontHeader, XBrushes.Black, xStart + 250, yPoint);
            gfx.DrawString("Amount", fontHeader, XBrushes.Black, xStart + 300, yPoint);
            gfx.DrawString("Status", fontHeader, XBrushes.Black, xStart + 360, yPoint);
            gfx.DrawString("SubmittedOn", fontHeader, XBrushes.Black, xStart + 430, yPoint);

            yPoint += rowHeight; // This moves to the next row
        }

        // This writes the report title
        gfx.DrawString("Claims Report", new XFont("Arial", 20, XFontStyle.Bold), XBrushes.Black, new XPoint(40, 30));

        DrawHeaders(); // This draws the header row

        // This loops through each claim and draw it in the PDF
        foreach (var claim in claimsList)
        {
            string monthFormatted = claim.Month;
            var parts = claim.Month.Split(' ');
            if (parts.Length == 2 && int.TryParse(parts[1], out int year))
            {
                monthFormatted = $"{parts[0]} {year % 10000}";
            }

            // This gets lecturer name or unknown if missing
            var lecturerName = claim.User != null
                ? $"{claim.User.FirstName} {claim.User.LastName}"
                : "Unknown";

            // This draws the claim data in columns
            gfx.DrawString(claim.ClaimID.ToString(), fontRow, XBrushes.Black, xStart, yPoint);
            gfx.DrawString(lecturerName, fontRow, XBrushes.Black, xStart + 50, yPoint);
            gfx.DrawString(monthFormatted, fontRow, XBrushes.Black, xStart + 200, yPoint);
            gfx.DrawString(claim.HoursWorked.ToString(), fontRow, XBrushes.Black, xStart + 250, yPoint);
            gfx.DrawString(claim.TotalAmount.ToString("C"), fontRow, XBrushes.Black, xStart + 300, yPoint);
            gfx.DrawString(claim.VerificationStatus.ToString(), fontRow, XBrushes.Black, xStart + 360, yPoint);
            gfx.DrawString(claim.SubmittedOn.ToShortDateString(), fontRow, XBrushes.Black, xStart + 430, yPoint);

            yPoint += rowHeight; 

            // This checks if the page is full and adds a new page if needed
            if (yPoint > page.Height - 50)
            {
                page = pdf.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPoint = 60;
                DrawHeaders(); 
            }
        }

        // This saves the PDF to memory and sends it as a file
        pdf.Save(stream, false);
        return File(stream.ToArray(), "application/pdf", "ClaimsReport.pdf");
    }
}
