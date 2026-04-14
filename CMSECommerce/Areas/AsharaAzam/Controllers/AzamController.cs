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
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(x => x.ITSNumber == model.ITSNumber);

            if (existing != null)
                return RedirectToAction(nameof(Certificate), new { itsNumber = existing.ITSNumber });

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
            var entry = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);

            if (entry == null)
                return NotFound();

            try
            {
                using var ms = new MemoryStream();

                float width = 420;
                float height = 595;

                using var document = SKDocument.CreatePdf(ms);
                using var canvas = document.BeginPage(width, height);

                // =========================
                // SAFE BACKGROUND
                // =========================
                canvas.Clear(new SKColor(252, 250, 245));

                // =========================
                // SAFE FONT LOADING (CRITICAL FIX)
                // =========================
                SKTypeface font;

                try
                {
                    var fontPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/fonts/Amiri-Regular.ttf"
                    );

                    if (System.IO.File.Exists(fontPath))
                        font = SKTypeface.FromFile(fontPath);
                    else
                        font = SKTypeface.FromFamilyName("Arial"); // SAFE FALLBACK
                }
                catch
                {
                    font = SKTypeface.FromFamilyName("Arial");
                }

                // =========================
                // WATERMARK (SAFE ALPHA)
                // =========================
                using (var wm = new SKPaint
                {
                    Color = new SKColor(120, 120, 120, 18), // SAFE (not too light or too strong)
                    TextSize = 28,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true,
                    Typeface = font
                })
                {
                    canvas.Save();
                    canvas.Translate(width / 2, height / 2);
                    canvas.RotateDegrees(-30);

                    canvas.DrawText("ANJUMAN E BURHANI", 0, 0, wm);
                    canvas.DrawText("HUSSAINI ALAM", 0, 40, wm);

                    canvas.Restore();
                }

                // =========================
                // BORDER
                // =========================
                using var border = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 4,
                    Color = new SKColor(212, 175, 55),
                    IsAntialias = true
                };

                canvas.DrawRect(12, 12, width - 24, height - 24, border);

                // =========================
                // HEADER
                // =========================
                using var header = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 20,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true,
                    Typeface = font
                };

                canvas.DrawText("ASHARA AZAM CERTIFICATE", width / 2, 90, header);

                // =========================
                // NAME (SAFE)
                // =========================
                using var namePaint = new SKPaint
                {
                    Color = new SKColor(184, 134, 11),
                    TextSize = 24,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true,
                    Typeface = font,
                    FakeBoldText = true
                };

                float nameY = 180;

                canvas.DrawText(entry.FullName, width / 2, nameY, namePaint);

                // =========================
                // ITS
                // =========================
                using var body = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true,
                    Typeface = font
                };

                canvas.DrawText($"ITS ID: {entry.ITSNumber}", width / 2, 220, body);

                // =========================
                // CONSENT (UNCHANGED)
                // =========================
                float y = 270;
                float gap = 40;

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y, body);
                canvas.DrawText("Mein Maro Business 100% close raakhis.", width / 2, y + 15, body);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap, body);
                canvas.DrawText("Mein Job Si Raza Lay-Lais.", width / 2, y + gap + 15, body);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap * 2, body);
                canvas.DrawText("Mein Studies Si Raza Lay-Lais.", width / 2, y + gap * 2 + 15, body);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap * 3, body);
                canvas.DrawText("Qablal Waqt Majlis Ma Hazir Rahis.", width / 2, y + gap * 3 + 15, body);

                // =========================
                // FOOTER
                // =========================
                using var footer = new SKPaint
                {
                    Color = new SKColor(0, 102, 102),
                    TextSize = 11,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true,
                    Typeface = font
                };

                canvas.DrawText("Anjuman E Burhani", width / 2, height - 70, footer);
                canvas.DrawText("Hyderabad - Hussaini Alam", width / 2, height - 50, footer);
                canvas.DrawText($"Dated: {entry.CreatedDate:dd MMM yyyy}", width / 2, height - 30, footer);

                document.EndPage();
                document.Close();

                // =========================
                // CRITICAL FIX: RESET STREAM
                // =========================
                ms.Position = 0;

                return File(ms.ToArray(), "application/pdf",
                    $"{entry.ITSNumber}_Certificate.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "PDF Generation Error: " + ex.Message);
            }
        }
    }
}