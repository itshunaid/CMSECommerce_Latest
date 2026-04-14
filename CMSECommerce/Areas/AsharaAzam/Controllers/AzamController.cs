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

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AzamEntryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AzamEntryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Check if ITS already registered
            var existingEntry = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(e => e.ITSNumber == model.ITSNumber);

            if (existingEntry != null)
            {
                // Download existing certificate immediately
                return RedirectToAction(nameof(Certificate), new { itsNumber = existingEntry.ITSNumber });
            }

            // 2. New Registration
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

        public async Task<IActionResult> Certificate(string itsNumber)
        {
            var entry = await _context.AsharaAzamEntries.FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);
            if (entry == null) return NotFound();

            try
            {
                using (var ms = new MemoryStream())
                {
                    // Standard A5 Size in Points: 420 (width) x 595 (height)
                    float width = 420;
                    float height = 595;

                    using (var document = SKDocument.CreatePdf(ms))
                    using (var canvas = document.BeginPage(width, height))
                    {
                        // 1. Decorative Gold Border
                        using var borderPaint = new SKPaint
                        {
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 8,
                            Color = new SKColor(197, 160, 89), // Matte Gold
                            IsAntialias = true
                        };
                        canvas.DrawRect(15, 15, width - 30, height - 30, borderPaint);

                        // 2. Header Section
                        using var paint = new SKPaint
                        {
                            Color = SKColors.Black,
                            TextSize = 22,
                            IsAntialias = true,
                            FakeBoldText = true,
                            TextAlign = SKTextAlign.Center,
                            Typeface = SKTypeface.FromFamilyName("serif")
                        };
                        canvas.DrawText("ASHARA AZAM", width / 2, 80, paint);

                        paint.TextSize = 16;
                        canvas.DrawText("CERTIFICATE", width / 2, 105, paint);

                        // 3. Certification Body
                        paint.TextSize = 12;
                        paint.FakeBoldText = false;
                        paint.Color = SKColors.Gray;
                        canvas.DrawText("This is to certify the Niyyat of", width / 2, 160, paint);

                        paint.TextSize = 20;
                        paint.Color = new SKColor(148, 116, 54); // Deep Gold
                        paint.FakeBoldText = true;
                        canvas.DrawText(entry.FullName.ToUpper(), width / 2, 195, paint);

                        paint.TextSize = 14;
                        paint.Color = SKColors.Black;
                        paint.FakeBoldText = false;
                        canvas.DrawText($"ITS ID: {entry.ITSNumber}", width / 2, 225, paint);

                        // 4. The 4 Niyyat Points
                        paint.TextSize = 11;
                        paint.Color = new SKColor(30, 30, 30);

                        float startY = 300;
                        float lineSpacing = 45; // Space between bullet points

                        // Points are split into two lines for readability on mobile
                        canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY, paint);
                        canvas.DrawText("Mein Maro Business 100% close raakhis.", width / 2, startY + 15, paint);

                        canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY + lineSpacing, paint);
                        canvas.DrawText("Mein Job Si Raza Lay-Lais.", width / 2, startY + lineSpacing + 15, paint);

                        canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY + (lineSpacing * 2), paint);
                        canvas.DrawText("Mein Studies Si Raza Lay-Lais.", width / 2, startY + (lineSpacing * 2) + 15, paint);

                        canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, startY + (lineSpacing * 3), paint);
                        canvas.DrawText("Qablal Waqt Majlis Ma Hazir Rahis.", width / 2, startY + (lineSpacing * 3) + 15, paint);

                        // 5. Minimal Footer
                        paint.TextSize = 12;
                        paint.Color = new SKColor(148, 116, 54);
                        canvas.DrawText($"Dated: {entry.CreatedDate:dd MMM yyyy}", width / 2, height - 50, paint);

                        document.EndPage();
                        document.Close();
                    }

                    // Return the PDF - uses A5 dimensions (420x595)
                    return File(ms.ToArray(), "application/pdf", $"{entry.ITSNumber}_AsharaAzam.pdf");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Generation Error: " + ex.Message);
            }
        }
    }
}