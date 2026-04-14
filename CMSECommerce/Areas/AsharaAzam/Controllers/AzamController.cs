using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using QuestPDF.Helpers;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        public async Task<IActionResult> GenerateAsharaAzamOld(string itsNumber)
        {
            var entry = await _context.AsharaAzamEntries.FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);
            if (entry == null) return NotFound();

            using var ms = new MemoryStream();
            float width = 842;
            float height = 595;

            using var document = SKDocument.CreatePdf(ms);
            SKCanvas canvas = null;

            // Palette
            var primaryTeal = new SKColor(0, 78, 80);
            var accentGold = new SKColor(197, 159, 70);
            var textDark = new SKColor(50, 50, 50);

            // FONT LOADING LOGIC (FIXED)
            SKTypeface GetFont(bool bold)
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "OpenSans-Regular.ttf");
                if (System.IO.File.Exists(fontPath))
                {
                    var tf = SKTypeface.FromFile(fontPath);
                    return bold ? SKTypeface.FromFamilyName(tf.FamilyName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) : tf;
                }
                return SKTypeface.FromFamilyName(null, bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            }

            var fontRegular = GetFont(false);
            var fontBold = GetFont(true);

            canvas = document.BeginPage(width, height);
            canvas.Clear(new SKColor(252, 251, 247));

            float centerX = width / 2;
            float currentY = 160; // Adjusted starting point for title

            // LOGO
            try
            {
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    using var stream = System.IO.File.OpenRead(logoPath);
                    using var bitmap = SKBitmap.Decode(stream);
                    canvas.DrawBitmap(bitmap, new SKRect(centerX - 40, 30, centerX + 40, 110));
                }
            }
            catch { }

            // TITLE
            using (var p = new SKPaint { Typeface = fontBold, TextSize = 32, Color = primaryTeal, TextAlign = SKTextAlign.Center, IsAntialias = true })
            {
                canvas.DrawText("MY ASHARA AZAM", centerX, currentY, p);
            }

            // ACKNOWLEDGMENT
            currentY += 45;
            using (var p = new SKPaint { Typeface = fontRegular, TextSize = 15, Color = new SKColor(100, 100, 100), TextAlign = SKTextAlign.Center, IsAntialias = true })
            {
                canvas.DrawText("Acknowledging the sincere Niyyat of", centerX, currentY, p);
                currentY += 35;
                p.Typeface = fontBold; p.TextSize = 30; p.Color = accentGold;
                canvas.DrawText(entry.FullName.ToUpper(), centerX, currentY, p);
                currentY += 30;
                p.Typeface = fontRegular; p.TextSize = 16; p.Color = primaryTeal;
                canvas.DrawText($"ITS ID: {entry.ITSNumber}", centerX, currentY, p);
            }

            // CONSENT HEADER
            currentY += 65;
            using (var p = new SKPaint { Typeface = fontBold, TextSize = 17, Color = primaryTeal, TextAlign = SKTextAlign.Center, IsAntialias = true })
            {
                canvas.DrawText("Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein:", centerX, currentY, p);
            }

            // BULLETS
            currentY += 45;
            string[] items = { "Maro Business 100% close raakhis.", "Job Si Raza Lay-Lais.", "Studies Si Raza Lay-Lais.", "Qablal Waqt Waaz ni Majlis Ma Hazir Rahis." };
            using (var textP = new SKPaint { Typeface = fontRegular, TextSize = 15, Color = textDark, IsAntialias = true })
            using (var bulletP = new SKPaint { Typeface = fontBold, TextSize = 15, Color = accentGold, IsAntialias = true })
            {
                float margin = centerX - 180;
                foreach (var item in items)
                {
                    canvas.DrawText("•", margin, currentY, bulletP);
                    canvas.DrawText(item, margin + 20, currentY, textP);
                    currentY += 30;
                }
            }

            // FOOTER
            string footerLine = $"ANJUMAN E BURHANI  •  Hussaini Alam, Hyderabad  •  Issued: {DateTime.Now:dd MMM yyyy}";
            using (var p = new SKPaint { Typeface = fontRegular, TextSize = 10, Color = primaryTeal.WithAlpha(180), TextAlign = SKTextAlign.Center, IsAntialias = true })
            {
                canvas.DrawText(footerLine, centerX, height - 55, p);
            }

            document.EndPage();
            document.Close();
            return File(ms.ToArray(), "application/pdf", $"AsharaAzam_{entry.ITSNumber}.pdf");
        }
        public async Task<IActionResult> GenerateAsharaAzam(string itsNumber)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var entry = await _context.AsharaAzamEntries.FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);
            if (entry == null) return NotFound();

            // 1. Exact Image Dimensions in Points (1024x722px / 1.33)
            var customPageSize = new PageSize(768, 542);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(customPageSize);
                    page.Margin(0);

                    // 2. Background Layer - Locked to Page Size
                    page.Background().Element(output =>
                    {
                        var bgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "floral-bg.jpg");
                        if (System.IO.File.Exists(bgPath))
                        {
                            // FitArea() ensures the image matches the 768x542 canvas exactly
                            output.Image(bgPath).FitArea();
                        }
                        else
                        {
                            output.Background("#FCFBF7");
                        }
                    });

                    // 3. Uplifted Content Layer
                    page.Content()
                        .AlignCenter()
                        .AlignTop()
                        .PaddingTop(55)
                        .PaddingHorizontal(180)
                        .PaddingBottom(30)
                        .Column(mainCol =>
                        {
                            // Logo
                            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
                            if (System.IO.File.Exists(logoPath))
                            {
                                mainCol.Item().AlignCenter().Width(45).Image(logoPath);
                            }

                            // Tight Header Section (Padding removed between lines)
                            mainCol.Item().PaddingTop(8).AlignCenter().Text("MY ASHARA AZAM")
                            .FontFamily(Fonts.Verdana).FontSize(20).ExtraBold().FontColor("#004E50");

                            mainCol.Item().PaddingTop(5).AlignCenter().Text("Acknowledging the sincere Niyyat of")
                            .FontSize(11).Italic().FontColor("#666666");

                            // Name & ID
                            mainCol.Item().PaddingTop(2).AlignCenter().Text(entry.FullName.ToUpper())
                            .FontFamily(Fonts.Georgia).FontSize(26).Bold().FontColor("#C59F46");

                            mainCol.Item().AlignCenter().Text($"ITS ID: {entry.ITSNumber}")
                            .FontSize(13).Medium().FontColor("#004E50");

                            // Commitment Header (Tightened to ID)
                            mainCol.Item().PaddingTop(5).AlignCenter()
                            .Text("Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein:")
                            .FontSize(14).SemiBold().FontColor("#004E50");

                            // Content List
                            mainCol.Item().PaddingTop(12).PaddingLeft(25).Column(listCol =>
                            {
                                string[] items = {
                        "Maro Business 100% close raakhis.",
                        "Job Si Raza Lay-Lais.",
                        "Studies Si Raza Lay-Lais.",
                        "Qablal Waqt Waaz ni Majlis Ma Hazir Rahis."
                            };

                                foreach (var item in items)
                                {
                                    listCol.Item().PaddingBottom(4).Row(row =>
                                    {
                                        row.ConstantItem(20).Text("•").FontSize(16).FontColor("#C59F46");
                                        row.RelativeItem().PaddingTop(2).Text(item)
                                        .FontSize(12).FontColor("#333333").LineHeight(1.1f);
                                    });
                                }
                            });

                            // Centered Footer
                            mainCol.Item().AlignBottom().PaddingBottom(10).AlignCenter().Column(ft =>
                            {
                                ft.Item().AlignCenter().Text("ANJUMAN E BURHANI")
                                .Bold().FontSize(10).FontColor("#004E50");

                                ft.Item().AlignCenter().Text("Hussaini Alam, Hyderabad")
                                .FontSize(9).FontColor("#666666");

                                ft.Item().AlignCenter().PaddingTop(2).Text($"DATE: {DateTime.Now:dd MMM yyyy}")
                                .Bold().FontSize(9).FontColor("#004E50");
                            });
                        });
                });
            });

            byte[] pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"AsharaAzam_{entry.ITSNumber}.pdf");
        }
    }
}