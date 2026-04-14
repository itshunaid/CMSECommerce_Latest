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

                // =====================================================
                // 🌙 BACKGROUND (DO FIRST)
                // =====================================================
                canvas.Clear(new SKColor(252, 250, 245));

                // =====================================================
                // 🌫 LIGHT WATERMARK (VERY IMPORTANT FIX)
                // =====================================================
                using (var wm = new SKPaint
                {
                    Color = new SKColor(120, 120, 120, 12),
                    TextSize = 28,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.Save();
                    canvas.Translate(width / 2, height / 2);
                    canvas.RotateDegrees(-30);

                    canvas.DrawText("ANJUMAN E BURHANI", 0, 0, wm);
                    canvas.DrawText("HYDERABAD HUSSAINI ALAM", 0, 40, wm);

                    canvas.Restore();
                }

                // =====================================================
                // 🟡 BORDER (SAFE)
                // =====================================================
                using var borderGold = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 5,
                    Color = new SKColor(212, 175, 55),
                    IsAntialias = true
                };

                using var borderTeal = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f,
                    Color = new SKColor(0, 102, 102),
                    IsAntialias = true
                };

                canvas.DrawRect(10, 10, width - 20, height - 20, borderGold);
                canvas.DrawRect(20, 20, width - 40, height - 40, borderTeal);

                // =====================================================
                // 🌸 FLORAL CORNERS (MULTI COLOR)
                // =====================================================
                using var gold = new SKPaint { Color = new SKColor(212, 175, 55, 160), IsAntialias = true };
                using var teal = new SKPaint { Color = new SKColor(0, 102, 102, 140), IsAntialias = true };
                using var maroon = new SKPaint { Color = new SKColor(128, 0, 32, 120), IsAntialias = true };
                using var emerald = new SKPaint { Color = new SKColor(0, 128, 96, 120), IsAntialias = true };

                void Flower(float x, float y)
                {
                    canvas.DrawCircle(x, y, 10, gold);
                    canvas.DrawCircle(x + 8, y + 5, 6, teal);
                    canvas.DrawCircle(x - 8, y + 5, 6, maroon);
                    canvas.DrawCircle(x, y + 10, 6, emerald);
                }

                Flower(35, 35);
                Flower(width - 35, 35);
                Flower(35, height - 35);
                Flower(width - 35, height - 35);

                // =====================================================
                // 🏷 HEADER
                // =====================================================
                using var header = new SKPaint
                {
                    Color = new SKColor(40, 40, 40),
                    TextSize = 22,
                    FakeBoldText = true,
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true
                };

                canvas.DrawText("ASHARA AZAM", width / 2, 95, header);

                header.TextSize = 13;
                header.Color = new SKColor(0, 102, 102);
                canvas.DrawText("A Declaration of Faith & Commitment", width / 2, 118, header);

                // =====================================================
                // 📜 BODY INTRO
                // =====================================================
                using var body = new SKPaint
                {
                    Color = new SKColor(90, 90, 90),
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center
                };

                canvas.DrawText("This certificate respectfully acknowledges the Niyyat of", width / 2, 165, body);

                // =====================================================
                // 👤 NAME (SAFE + NO UNDERLINE)
                // =====================================================
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/Amiri-Regular.ttf");

                SKTypeface font = System.IO.File.Exists(fontPath)
                    ? SKTypeface.FromFile(fontPath)
                    : SKTypeface.FromFamilyName("serif");

                using var glow = new SKPaint
                {
                    Color = new SKColor(212, 175, 55, 35),
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

                string[] words = entry.FullName
                    .ToUpper()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                string line1 = "", line2 = "";

                foreach (var w in words)
                {
                    if (namePaint.MeasureText(line1 + " " + w) < maxWidth)
                        line1 += (line1 == "" ? w : " " + w);
                    else
                        line2 += (line2 == "" ? w : " " + w);
                }

                float nameY = 205;

                canvas.DrawText(line1.Trim(), width / 2 + 1, nameY + 1, glow);
                canvas.DrawText(line1.Trim(), width / 2, nameY, namePaint);

                if (!string.IsNullOrEmpty(line2))
                    canvas.DrawText(line2.Trim(), width / 2, nameY + 25, namePaint);

                // =====================================================
                // ITS (FIXED POSITION)
                // =====================================================
                body.Color = SKColors.Black;
                body.TextSize = 13;

                canvas.DrawText($"ITS ID: {entry.ITSNumber}", width / 2, 245, body);

                // =====================================================
                // 📌 CONSENT (SAFE POSITIONING FIX)
                // =====================================================
                using var txt = new SKPaint
                {
                    Color = new SKColor(40, 40, 40),
                    TextSize = 11,
                    TextAlign = SKTextAlign.Center
                };

                float y = 285;
                float gap = 38;

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y, txt);
                canvas.DrawText("Mein Maro Business 100% close raakhis.", width / 2, y + 14, txt);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap, txt);
                canvas.DrawText("Mein Job Si Raza Lay-Lais.", width / 2, y + gap + 14, txt);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap * 2, txt);
                canvas.DrawText("Mein Studies Si Raza Lay-Lais.", width / 2, y + gap * 2 + 14, txt);

                canvas.DrawText("• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma", width / 2, y + gap * 3, txt);
                canvas.DrawText("Qablal Waqt Majlis Ma Hazir Rahis.", width / 2, y + gap * 3 + 14, txt);

                // =====================================================
                // ✍ FOOTER
                // =====================================================
                using var footer = new SKPaint
                {
                    Color = new SKColor(0, 102, 102),
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center,
                    FakeBoldText = true
                };

                canvas.DrawText("Anjuman E Burhani", width / 2, height - 80, footer);

                footer.TextSize = 10;
                footer.FakeBoldText = false;
                footer.Color = new SKColor(184, 134, 11);

                canvas.DrawText("Hyderabad - Hussaini Alam", width / 2, height - 60, footer);
                canvas.DrawText($"Dated: {entry.CreatedDate:dd MMM yyyy}", width / 2, height - 40, footer);

                document.EndPage();
                document.Close();

                return File(ms.ToArray(), "application/pdf",
                    $"{entry.ITSNumber}_AsharaAzamCertificate.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}