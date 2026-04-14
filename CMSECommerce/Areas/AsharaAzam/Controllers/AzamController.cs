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

                // 🌙 Soft emotional background (ivory warm tone)
                canvas.Clear(new SKColor(252, 250, 245));

                // =========================
                // 🌙 SOFT ISLAMIC FRAME (DOUBLE BORDER)
                // =========================
                using var outer = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 5,
                    Color = new SKColor(184, 134, 11),
                    IsAntialias = true
                };

                using var inner = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f,
                    Color = new SKColor(0, 102, 102, 160),
                    IsAntialias = true
                };

                canvas.DrawRoundRect(12, 12, width - 24, height - 24, 18, 18, outer);
                canvas.DrawRoundRect(22, 22, width - 44, height - 44, 14, 14, inner);

                // =========================
                // 🌫 GENTLE WATERMARK (VERY SOFT)
                // =========================
                using (var wm = new SKPaint
                {
                    Color = new SKColor(120, 120, 120, 25),
                    TextSize = 30,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.Save();
                    canvas.Translate(width / 2, height / 2);
                    canvas.RotateDegrees(-30);

                    canvas.DrawText("ANJUMAN E BURHANI", 0, 0, wm);
                    canvas.DrawText("HYDERABAD HUSSAINI ALAM", 0, 45, wm);

                    canvas.Restore();
                }

                // =========================
                // 🌸 SOFT EMOTIONAL CORNERS (MINIMAL)
                // =========================
                using var gold = new SKPaint { Color = new SKColor(184, 134, 11, 120), IsAntialias = true };
                using var teal = new SKPaint { Color = new SKColor(0, 102, 102, 100), IsAntialias = true };

                void Flower(float x, float y)
                {
                    canvas.DrawCircle(x, y, 10, gold);
                    canvas.DrawCircle(x + 8, y + 5, 6, teal);
                    canvas.DrawCircle(x - 8, y + 5, 6, teal);
                    canvas.DrawCircle(x, y + 10, 5, gold);
                }

                Flower(35, 35);
                Flower(width - 35, 35);
                Flower(35, height - 35);
                Flower(width - 35, height - 35);

                // =========================
                // 🏷 HEADER (EMOTIONAL CENTERED DESIGN)
                // =========================
                using var header = new SKPaint
                {
                    Color = new SKColor(40, 40, 40),
                    TextSize = 22,
                    FakeBoldText = true,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true
                };

                canvas.DrawText("ASHARA AZAM", width / 2, 90, header);

                header.TextSize = 13;
                header.Color = new SKColor(0, 102, 102);
                canvas.DrawText("A Declaration of Faith, Commitment & Devotion", width / 2, 115, header);

                // =========================
                // 📜 BODY INTRO (EMOTIONAL TONE)
                // =========================
                using var body = new SKPaint
                {
                    Color = new SKColor(90, 90, 90),
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true
                };

                canvas.DrawText("This certificate respectfully acknowledges the sincere Niyyat of", width / 2, 170, body);

                // =========================
                // 👤 NAME (SOFT GLOW - NO UNDERLINE)
                // =========================
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/Amiri-Regular.ttf");

                SKTypeface font = System.IO.File.Exists(fontPath)
                    ? SKTypeface.FromFile(fontPath)
                    : SKTypeface.FromFamilyName("serif");

                using var glow = new SKPaint
                {
                    Color = new SKColor(184, 134, 11, 40),
                    TextSize = 30,
                    TextAlign = SKTextAlign.Center,
                    Typeface = font,
                    IsAntialias = true
                };

                using var namePaint = new SKPaint
                {
                    Color = new SKColor(184, 134, 11),
                    TextSize = 26,
                    FakeBoldText = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = font,
                    IsAntialias = true
                };

                float maxWidth = width - 90;

                string[] words = entry.FullName.ToUpper().Split(' ');
                string line1 = "", line2 = "";

                foreach (var w in words)
                {
                    if (namePaint.MeasureText(line1 + " " + w) < maxWidth)
                        line1 += (line1 == "" ? w : " " + w);
                    else
                        line2 += (line2 == "" ? w : " " + w);
                }

                float nameY = 210;

                canvas.DrawText(line1.Trim(), width / 2 + 1, nameY + 1, glow);
                canvas.DrawText(line1.Trim(), width / 2, nameY, namePaint);

                if (!string.IsNullOrEmpty(line2))
                    canvas.DrawText(line2.Trim(), width / 2, nameY + 25, namePaint);

                // =========================
                // ITS
                // =========================
                body.Color = SKColors.Black;
                body.TextSize = 13;

                canvas.DrawText($"ITS ID: {entry.ITSNumber}", width / 2, 255, body);

                // =========================
                // 📌 CONSENT (UNCHANGED)
                // =========================
                using var txt = new SKPaint
                {
                    Color = new SKColor(40, 40, 40),
                    TextSize = 11,
                    TextAlign = SKTextAlign.Center
                };

                float y = 300;
                float gap = 45;

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y, txt);
                canvas.DrawText("Mein Maro Business 100% close raakhis.", width / 2, y + 15, txt);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap, txt);
                canvas.DrawText("Mein Job Si Raza Lay-Lais.", width / 2, y + gap + 15, txt);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap * 2, txt);
                canvas.DrawText("Mein Studies Si Raza Lay-Lais.", width / 2, y + gap * 2 + 15, txt);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap * 3, txt);
                canvas.DrawText("Qablal Waqt Majlis Ma Hazir Rahis.", width / 2, y + gap * 3 + 15, txt);

                // =========================
                // ✍ FOOTER (EMOTIONAL SIGN OFF)
                // =========================
                using var footer = new SKPaint
                {
                    Color = new SKColor(0, 102, 102),
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center,
                    FakeBoldText = true
                };

                canvas.DrawText("With sincere prayers from Anjuman E Burhani", width / 2, height - 80, footer);

                footer.TextSize = 10;
                footer.FakeBoldText = false;
                footer.Color = new SKColor(184, 134, 11);

                canvas.DrawText("Hyderabad - Hussaini Alam", width / 2, height - 60, footer);
                canvas.DrawText($"Dated: {entry.CreatedDate:dd MMM yyyy}", width / 2, height - 40, footer);

                document.EndPage();
                document.Close();

                return File(ms.ToArray(), "application/pdf",
                    $"{entry.ITSNumber}_EmotionalPremiumCertificate.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}