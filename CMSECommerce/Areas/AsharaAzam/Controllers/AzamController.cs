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
                return RedirectToAction(nameof(GenerateAsharaAzam), new { itsNumber = existing.ITSNumber });

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

            return RedirectToAction(nameof(GenerateAsharaAzam), new { itsNumber = entry.ITSNumber });
        }

        public async Task<IActionResult> GenerateAsharaAzam(string itsNumber)
        {
            var entry = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);

            if (entry == null) return NotFound();

            using var ms = new MemoryStream();

            float width = 595;
            float height = 842;

            using var document = SKDocument.CreatePdf(ms);
            using var canvas = document.BeginPage(width, height);

            // =====================================================
            // 🎨 BACKGROUND
            // =====================================================
            canvas.Clear(new SKColor(252, 251, 247));

            float margin = 55;
            float centerX = width / 2;

            // =====================================================
            // 🖼️ LOGO RESERVED SPACE (IMPORTANT FIX)
            // =====================================================
            float logoBlockHeight = 140;
            float logoSize = 95;
            float logoTopPadding = 35;

            float y = logoBlockHeight + 20;

            try
            {
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");

                if (System.IO.File.Exists(logoPath))
                {
                    using var stream = new FileStream(logoPath, FileMode.Open);
                    using var bitmap = SKBitmap.Decode(stream);

                    var rect = new SKRect(
                        centerX - logoSize / 2,
                        logoTopPadding,
                        centerX + logoSize / 2,
                        logoTopPadding + logoSize
                    );

                    canvas.DrawBitmap(bitmap, rect, new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High
                    });
                }
            }
            catch { }

            // =====================================================
            // 🔠 FONT SETUP
            // =====================================================
            SKTypeface font;
            try
            {
                var fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/Amiri-Regular.ttf");
                font = System.IO.File.Exists(fontPath)
                    ? SKTypeface.FromFile(fontPath)
                    : SKTypeface.FromFamilyName("Georgia");
            }
            catch
            {
                font = SKTypeface.FromFamilyName("Arial");
            }

            float centerY = height / 2;

            // =====================================================
            // 📏 AUTO FONT ENGINE
            // =====================================================
            float AutoFont(float baseSize, string text, float maxWidth, bool bold = false)
            {
                float size = baseSize;

                using var paint = new SKPaint
                {
                    Typeface = font,
                    TextSize = size,
                    FakeBoldText = bold
                };

                while (size > 8 && paint.MeasureText(text) > maxWidth)
                {
                    size -= 0.5f;
                    paint.TextSize = size;
                }

                return size;
            }

            // =====================================================
            // 🧠 TEXT ENGINE (AUTO WRAP + CENTER)
            // =====================================================
            void DrawText(string text, float size, SKColor color, bool bold = false, float spacing = 14)
            {
                float maxWidth = width - (margin * 2);

                using var paint = new SKPaint
                {
                    Typeface = font,
                    TextSize = AutoFont(size, text, maxWidth, bold),
                    Color = color,
                    IsAntialias = true,
                    FakeBoldText = bold,
                    TextAlign = SKTextAlign.Center
                };

                var words = text.Split(' ');
                string line = "";

                foreach (var word in words)
                {
                    var test = string.IsNullOrEmpty(line) ? word : line + " " + word;

                    if (paint.MeasureText(test) > maxWidth)
                    {
                        canvas.DrawText(line, centerX, y, paint);
                        y += paint.TextSize + 6;
                        line = word;
                    }
                    else
                    {
                        line = test;
                    }
                }

                if (!string.IsNullOrEmpty(line))
                {
                    canvas.DrawText(line, centerX, y, paint);
                    y += paint.TextSize + spacing;
                }
            }

            // =====================================================
            // 🟡 BORDER DESIGN
            // =====================================================
            void DrawBorder()
            {
                using var p = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true };

                p.Color = new SKColor(184, 134, 11);
                p.StrokeWidth = 6;
                canvas.DrawRoundRect(margin - 20, 30, width - (margin * 2) + 40, height - 60, 12, 12, p);

                p.Color = new SKColor(0, 70, 70);
                p.StrokeWidth = 2;
                canvas.DrawRoundRect(margin - 10, 40, width - (margin * 2) + 20, height - 80, 10, 10, p);
            }

            DrawBorder();

            // =====================================================
            // 🏷️ TITLE
            // =====================================================
            DrawText("MY ASHARA AZAM", 34, new SKColor(0, 70, 70), true, 10);

            // =====================================================
            // 💫 FAITH MESSAGE (HIGHLIGHTED)
            // =====================================================
            using (var paint = new SKPaint
            {
                Color = new SKColor(212, 175, 55, 60),
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(margin, y - 30, width - (margin * 2), 45, 10, 10, paint);
            }

            DrawText("A Sacred Declaration of Faith & Commitment", 16,
                new SKColor(120, 0, 0), true, 20);

            // =====================================================
            // 👤 DETAILS
            // =====================================================
            DrawText("Acknowledging the sincere Niyyat of", 12, SKColors.DimGray);

            DrawText(entry.FullName.ToUpper(), 30, new SKColor(184, 134, 11), true, 6);

            DrawText($"ITS ID: {entry.ITSNumber}", 12, SKColors.Black, true, 25);

            // =====================================================
            // 📜 CONSENT LINES (UNCHANGED - DO NOT MODIFY)
            // =====================================================
            string[] lines = {
                "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein Maro Business 100% close raakhis.",
                "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein Job Si Raza Lay-Lais.",
                "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein Studies Si Raza Lay-Lais.",
                "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Qablal Waqt Majlis Ma Hazir Rahis."
            };

            foreach (var line in lines)
            {
                DrawText(line, 12, new SKColor(45, 45, 45), false, 12);
            }

            // =====================================================
            // 📌 FOOTER
            // =====================================================
            y = height - 120;

            DrawText("ANJUMAN E BURHANI", 14, new SKColor(0, 70, 70), true, 5);
            DrawText("Hussaini Alam, Hyderabad", 11, new SKColor(184, 134, 11));
            DrawText($"Issued on {entry.CreatedDate:dd MMM yyyy}", 10, SKColors.Gray);

            document.EndPage();
            document.Close();

            return File(ms.ToArray(), "application/pdf",
                $"AsharaAzam_{entry.ITSNumber}.pdf");
        }
    }
}