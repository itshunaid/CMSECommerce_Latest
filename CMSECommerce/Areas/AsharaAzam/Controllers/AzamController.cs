using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CMSECommerce.Areas.AsharaAzam.Controllers
{
    [Area("AsharaAzam")]
    public class AzamController : Controller
    {
        private readonly DataContext _context;

        public AzamController(DataContext context)
        {
            _context = context;
        }

        // =========================
        // 📥 GET FORM
        // =========================
        [HttpGet]
        public IActionResult Index()
        {
            return View(new AzamEntryViewModel());
        }

        // =========================
        // 📤 POST FORM
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AzamEntryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check duplicate ITS
            var existingEntry = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(e => e.ITSNumber == model.ITSNumber);

            if (existingEntry != null)
            {
                return RedirectToAction(nameof(Certificate), new { itsNumber = existingEntry.ITSNumber });
            }

            // Save new record
            var entry = new AsharaAzamEntry
            {
                ITSNumber = model.ITSNumber,
                FullName = model.FullName,
                ConsentGiven = model.ConsentGiven,
                ConsentMessage = "Azam Niyyat 4 Points Confirmed",
                CreatedDate = DateTime.Now
            };

            _context.AsharaAzamEntries.Add(entry);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Certificate), new { itsNumber = entry.ITSNumber });
        }

        // =========================
        // 🧾 GENERATE CERTIFICATE PDF
        // =========================
        public async Task<IActionResult> Certificate(string itsNumber)
        {
            var entry = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);

            if (entry == null)
                return NotFound();

            try
            {
                using var ms = new MemoryStream();

                float width = 420;   // A5 Width
                float height = 595;  // A5 Height

                using var document = SKDocument.CreatePdf(ms);
                using var canvas = document.BeginPage(width, height);

                canvas.Clear(SKColors.White);

                // =========================
                // 🌫 WATERMARK
                // =========================
                using (var watermarkPaint = new SKPaint
                {
                    Color = new SKColor(180, 180, 180, 50),
                    TextSize = 36,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("serif"),
                    FakeBoldText = true
                })
                {
                    canvas.Save();
                    canvas.Translate(width / 2, height / 2);
                    canvas.RotateDegrees(-30);

                    canvas.DrawText("Anjuman E Burhani", 0, 0, watermarkPaint);
                    canvas.DrawText("Hyderabad Hussaini Alam", 0, 50, watermarkPaint);

                    canvas.Restore();
                }

                // =========================
                // 🟡 DOUBLE GOLD BORDER
                // =========================
                using (var outerBorder = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 6,
                    Color = new SKColor(212, 175, 55),
                    IsAntialias = true
                })
                using (var innerBorder = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    Color = new SKColor(212, 175, 55),
                    IsAntialias = true
                })
                {
                    canvas.DrawRect(10, 10, width - 20, height - 20, outerBorder);
                    canvas.DrawRect(20, 20, width - 40, height - 40, innerBorder);
                }

                // =========================
                // 🏷 HEADER
                // =========================
                using var headerPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    FakeBoldText = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("serif")
                };

                canvas.DrawText("ASHARA AZAM", width / 2, 90, headerPaint);

                headerPaint.TextSize = 16;
                canvas.DrawText("CERTIFICATE", width / 2, 115, headerPaint);

                // =========================
                // 📜 BODY
                // =========================
                using var bodyPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("serif")
                };

                canvas.DrawText("This is to certify the Niyyat of", width / 2, 170, bodyPaint);

                using var namePaint = new SKPaint
                {
                    Color = new SKColor(148, 116, 54),
                    TextSize = 22,
                    FakeBoldText = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("serif")
                };

                canvas.DrawText(entry.FullName.ToUpper(), width / 2, 205, namePaint);

                bodyPaint.Color = SKColors.Black;
                bodyPaint.TextSize = 13;
                canvas.DrawText($"ITS ID: {entry.ITSNumber}", width / 2, 235, bodyPaint);

                // =========================
                // 📌 ORIGINAL NIYYAT LINES (UNCHANGED)
                // =========================
                using var niyyatPaint = new SKPaint
                {
                    Color = new SKColor(30, 30, 30),
                    TextSize = 11,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("serif")
                };

                float startY = 300;
                float lineSpacing = 45;

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY, niyyatPaint);
                canvas.DrawText("Mein Maro Business 100% close raakhis.", width / 2, startY + 15, niyyatPaint);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY + lineSpacing, niyyatPaint);
                canvas.DrawText("Mein Job Si Raza Lay-Lais.", width / 2, startY + lineSpacing + 15, niyyatPaint);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY + (lineSpacing * 2), niyyatPaint);
                canvas.DrawText("Mein Studies Si Raza Lay-Lais.", width / 2, startY + (lineSpacing * 2) + 15, niyyatPaint);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY + (lineSpacing * 3), niyyatPaint);
                canvas.DrawText("Qablal Waqt Majlis Ma Hazir Rahis.", width / 2, startY + (lineSpacing * 3) + 15, niyyatPaint);

                // =========================
                // ✍ FOOTER
                // =========================
                using var footerPaint = new SKPaint
                {
                    Color = new SKColor(148, 116, 54),
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("serif"),
                    FakeBoldText = true
                };

                canvas.DrawText("Anjuman E Burhani", width / 2, height - 80, footerPaint);

                footerPaint.TextSize = 10;
                footerPaint.FakeBoldText = false;
                canvas.DrawText("Hyderabad - Hussaini Alam", width / 2, height - 60, footerPaint);

                canvas.DrawText($"Dated: {entry.CreatedDate:dd MMM yyyy}", width / 2, height - 40, footerPaint);

                document.EndPage();
                document.Close();

                return File(ms.ToArray(), "application/pdf", $"{entry.ITSNumber}_PremiumCertificate.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error generating certificate: " + ex.Message);
            }
        }
    }
}