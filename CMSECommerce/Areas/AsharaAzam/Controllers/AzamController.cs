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

                canvas.Clear(SKColors.White);

                // =========================
                // 🌫 WATERMARK
                // =========================
                using (var wmPaint = new SKPaint
                {
                    Color = new SKColor(180, 180, 180, 50),
                    TextSize = 36,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center,
                    FakeBoldText = true
                })
                {
                    canvas.Save();
                    canvas.Translate(width / 2, height / 2);
                    canvas.RotateDegrees(-30);

                    canvas.DrawText("Anjuman E Burhani", 0, 0, wmPaint);
                    canvas.DrawText("Hyderabad Hussaini Alam", 0, 50, wmPaint);

                    canvas.Restore();
                }

                // =========================
                // 🌸 FLORAL DESIGN
                // =========================
                using var flowerPaint = new SKPaint
                {
                    Color = new SKColor(212, 175, 55, 120),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                void DrawFlower(float cx, float cy, float r)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        double angle = i * Math.PI / 3;
                        float x = cx + (float)(Math.Cos(angle) * r);
                        float y = cy + (float)(Math.Sin(angle) * r);
                        canvas.DrawCircle(x, y, r / 2, flowerPaint);
                    }
                    canvas.DrawCircle(cx, cy, r / 2, flowerPaint);
                }

                DrawFlower(40, 40, 20);
                DrawFlower(70, 70, 12);
                DrawFlower(width - 40, height - 40, 20);
                DrawFlower(width - 70, height - 70, 12);

                // =========================
                // ✨ CURVES
                // =========================
                using var curvePaint = new SKPaint
                {
                    Color = new SKColor(212, 175, 55, 60),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true
                };

                var path = new SKPath();
                path.MoveTo(0, 150);
                path.QuadTo(width / 2, 200, width, 150);
                canvas.DrawPath(path, curvePaint);

                var path2 = new SKPath();
                path2.MoveTo(0, height - 150);
                path2.QuadTo(width / 2, height - 200, width, height - 150);
                canvas.DrawPath(path2, curvePaint);

                // =========================
                // 🟡 BORDER
                // =========================
                using var outer = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 6, Color = new SKColor(212, 175, 55) };
                using var inner = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2, Color = new SKColor(212, 175, 55) };

                canvas.DrawRect(10, 10, width - 20, height - 20, outer);
                canvas.DrawRect(20, 20, width - 40, height - 40, inner);

                // =========================
                // 🏷 HEADER
                // =========================
                using var header = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    FakeBoldText = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("serif")
                };

                canvas.DrawText("ASHARA AZAM", width / 2, 90, header);
                header.TextSize = 16;
                canvas.DrawText("CERTIFICATE", width / 2, 115, header);

                // =========================
                // 📜 BODY
                // =========================
                using var body = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center
                };

                canvas.DrawText("This is to certify the Niyyat of", width / 2, 170, body);

                // =========================
                // 👤 NAME (AUTO FIT + ARABIC FONT)
                // =========================
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/Amiri-Regular.ttf");
                SKTypeface customFont = System.IO.File.Exists(fontPath)
                    ? SKTypeface.FromFile(fontPath)
                    : SKTypeface.FromFamilyName("serif");

                using var namePaint = new SKPaint
                {
                    Color = new SKColor(148, 116, 54),
                    TextSize = 26,
                    FakeBoldText = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = customFont,
                    IsAntialias = true
                };

                float maxWidth = width - 80;
                string[] words = entry.FullName.ToUpper().Split(' ');

                string line1 = "", line2 = "";

                foreach (var w in words)
                {
                    if (namePaint.MeasureText(line1 + " " + w) < maxWidth)
                        line1 += (line1 == "" ? w : " " + w);
                    else
                        line2 += (line2 == "" ? w : " " + w);
                }

                while (namePaint.MeasureText(line1) > maxWidth)
                    namePaint.TextSize--;

                float nameY = 205;
                canvas.DrawText(line1.Trim(), width / 2, nameY, namePaint);

                if (!string.IsNullOrEmpty(line2))
                    canvas.DrawText(line2.Trim(), width / 2, nameY + 25, namePaint);

                // ITS
                body.Color = SKColors.Black;
                body.TextSize = 13;
                canvas.DrawText($"ITS ID: {entry.ITSNumber}", width / 2, 250, body);

                // =========================
                // 📌 CONSENT LINES (UNCHANGED)
                // =========================
                using var txt = new SKPaint
                {
                    Color = new SKColor(30, 30, 30),
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
                // ✍ FOOTER
                // =========================
                using var footer = new SKPaint
                {
                    Color = new SKColor(148, 116, 54),
                    TextSize = 12,
                    TextAlign = SKTextAlign.Center,
                    FakeBoldText = true
                };

                canvas.DrawText("Anjuman E Burhani", width / 2, height - 80, footer);

                footer.TextSize = 10;
                footer.FakeBoldText = false;
                canvas.DrawText("Hyderabad - Hussaini Alam", width / 2, height - 60, footer);

                canvas.DrawText($"Dated: {entry.CreatedDate:dd MMM yyyy}", width / 2, height - 40, footer);

                document.EndPage();
                document.Close();

                return File(ms.ToArray(), "application/pdf", $"{entry.ITSNumber}_PremiumCertificate.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}