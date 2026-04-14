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

            using var ms = new MemoryStream();

            float width = 420;
            float height = 595;

            using var document = SKDocument.CreatePdf(ms);
            using var canvas = document.BeginPage(width, height);

            // =====================================================
            // 🎨 BACKGROUND (SOFT ISLAMIC PREMIUM TONE)
            // =====================================================
            canvas.Clear(new SKColor(250, 248, 242));

            // soft gold aura
            using (var aura = new SKPaint
            {
                Color = new SKColor(212, 175, 55, 18),
                IsAntialias = true
            })
            {
                canvas.DrawOval(width / 2, height / 2, 170, 240, aura);
            }

            // =====================================================
            // 🔐 SAFE FONT LOADING (SERVER PROOF)
            // =====================================================
            SKTypeface font;

            try
            {
                var fontPath = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot/fonts/Amiri-Regular.ttf");

                font = System.IO.File.Exists(fontPath)
                    ? SKTypeface.FromFile(fontPath)
                    : SKTypeface.FromFamilyName("Arial");
            }
            catch
            {
                font = SKTypeface.FromFamilyName("Arial");
            }

            // =====================================================
            // 🟡 PREMIUM BORDER (TRIPLE LAYER ARABIC STYLE)
            // =====================================================
            using var gold = new SKPaint { Color = new SKColor(212, 175, 55), Style = SKPaintStyle.Stroke, StrokeWidth = 5 };
            using var teal = new SKPaint { Color = new SKColor(0, 102, 102), Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var maroon = new SKPaint { Color = new SKColor(128, 0, 32), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

            canvas.DrawRoundRect(10, 10, width - 20, height - 20, 16, 16, gold);
            canvas.DrawRoundRect(18, 18, width - 36, height - 36, 14, 14, teal);
            canvas.DrawRoundRect(26, 26, width - 52, height - 52, 12, 12, maroon);

            // =====================================================
            // 🌸 PREMIUM FLORAL CORNERS (MULTI COLOR)
            // =====================================================
            void Flower(float x, float y)
            {
                canvas.DrawCircle(x, y, 9, gold);
                canvas.DrawCircle(x + 8, y + 5, 6, teal);
                canvas.DrawCircle(x - 8, y + 5, 6, maroon);
                canvas.DrawCircle(x, y + 10, 5, new SKPaint { Color = new SKColor(0, 128, 96) });
            }

            Flower(35, 35);
            Flower(width - 35, 35);
            Flower(35, height - 35);
            Flower(width - 35, height - 35);

            // =====================================================
            // 🌫 WATERMARK (SAFE - NEVER OVERPOWERS TEXT)
            // =====================================================
            using (var wm = new SKPaint
            {
                Color = new SKColor(120, 120, 120, 12),
                TextSize = 26,
                TextAlign = SKTextAlign.Center,
                Typeface = font,
                IsAntialias = true
            })
            {
                canvas.Save();
                canvas.Translate(width / 2, height / 2);
                canvas.RotateDegrees(-30);

                canvas.DrawText("ANJUMAN E BURHANI", 0, 0, wm);
                canvas.DrawText("HUSSAINI ALAM", 0, 40, wm);

                canvas.Restore();
            }

            // =====================================================
            // 📐 SAFE LAYOUT ENGINE (NO OVERLAP GUARANTEE)
            // =====================================================
            float cx = width / 2;
            float y = 95;

            void DrawText(string text, float size, SKColor color, bool bold = false)
            {
                using var p = new SKPaint
                {
                    TextSize = size,
                    Color = color,
                    TextAlign = SKTextAlign.Center,
                    Typeface = font,
                    FakeBoldText = bold,
                    IsAntialias = true
                };

                canvas.DrawText(text, cx, y, p);
                y += size + 10; // AUTO spacing (critical fix)
            }

            // =====================================================
            // 🏷 HEADER (PREMIUM ARABIC STYLE)
            // =====================================================
            DrawText("ASHARA AZAM CERTIFICATE", 20, SKColors.Black, true);
            DrawText("A Sacred Declaration of Faith & Commitment", 12, new SKColor(0, 102, 102));

            y += 10;

            DrawText("This certificate acknowledges the sincere Niyyat of", 11, new SKColor(90, 90, 90));

            y += 10;

            // =====================================================
            // 👤 NAME (FOCAL PREMIUM ELEMENT)
            // =====================================================
            using var namePaint = new SKPaint
            {
                TextSize = 24,
                Color = new SKColor(184, 134, 11),
                TextAlign = SKTextAlign.Center,
                Typeface = font,
                FakeBoldText = true,
                IsAntialias = true
            };

            canvas.DrawText(entry.FullName, cx, y, namePaint);
            y += 40;

            // =====================================================
            // ITS
            // =====================================================
            DrawText($"ITS ID: {entry.ITSNumber}", 12, SKColors.Black);

            y += 10;

            // =====================================================
            // 📌 CONSENT (UNCHANGED BUT SAFE FLOW)
            // =====================================================
            string[] lines =
            {
        "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma",
        "Mein Maro Business 100% close raakhis.",
        "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma",
        "Mein Job Si Raza Lay-Lais.",
        "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma",
        "Mein Studies Si Raza Lay-Lais.",
        "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma",
        "Qablal Waqt Majlis Ma Hazir Rahis."
    };

            foreach (var l in lines)
                DrawText(l, 10, new SKColor(40, 40, 40));

            // =====================================================
            // ✍ FOOTER (PREMIUM FINISH)
            // =====================================================
            y = height - 75;

            DrawText("Anjuman E Burhani", 12, new SKColor(0, 102, 102), true);
            y += 15;
            DrawText("Hyderabad - Hussaini Alam", 10, new SKColor(184, 134, 11));
            y += 15;
            DrawText($"Dated: {entry.CreatedDate:dd MMM yyyy}", 10, SKColors.Black);

            document.EndPage();
            document.Close();

            ms.Position = 0;

            return File(ms.ToArray(), "application/pdf",
                $"{entry.ITSNumber}_PremiumCertificate.pdf");
        }
    }
}