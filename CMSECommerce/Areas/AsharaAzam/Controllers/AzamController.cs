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

            // 1. Check if the record already exists
            var existing = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(x => x.ITSNumber == model.ITSNumber);

            if (existing != null)
            {
                // --- UPDATE LOGIC ---
                existing.FullName = model.FullName;
                existing.ConsentGiven = model.ConsentGiven;
                existing.ConsentMessage = "Azam Niyyat 4 Points Confirmed (Updated)";
                existing.CreatedDate = DateTime.Now; // Recommended to track modification

                _context.AsharaAzamEntries.Update(existing);
            }
            else
            {
                // --- INSERT LOGIC ---
                var entry = new AsharaAzamEntry
                {
                    ITSNumber = model.ITSNumber,
                    FullName = model.FullName,
                    ConsentGiven = model.ConsentGiven,
                    ConsentMessage = "Azam Niyyat 4 Points Confirmed",
                    CreatedDate = DateTime.Now
                };

                _context.AsharaAzamEntries.Add(entry);
            }

            // 2. Save changes (Works for both Add and Update)
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(GenerateAsharaAzam), new { itsNumber = model.ITSNumber });
        }

        public async Task<IActionResult> GenerateAsharaAzamOld(string itsNumber)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var entry = await _context.AsharaAzamEntries.FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);
            if (entry == null) return NotFound();

            // Exact Image Dimensions (1024x722px -> 768x542pt)
            var customPageSize = new PageSize(768, 542);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(customPageSize);
                    page.Margin(0);

                    // 1. Background Layer
                    page.Background().Element(output =>
                    {
                        var bgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "floral-bg.jpg");
                        if (System.IO.File.Exists(bgPath))
                        {
                            output.Image(bgPath).FitArea();
                        }
                        else
                        {
                            output.Background("#FCFBF7");
                        }
                    });

                    // 2. Main Content Layer
                    page.Content()
                        .PaddingHorizontal(180)
                        .Column(column =>
                        {
                            // Top Content Block - PUSHED DOWNWARD
                            // Changed PaddingTop from 45 to 85
                            column.Item().PaddingTop(85).Column(mainCol =>
                            {
                                // Large Logo
                                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
                                if (System.IO.File.Exists(logoPath))
                                {
                                    mainCol.Item().AlignCenter().Width(65).Image(logoPath);
                                }

                                // Tight Header Section
                                mainCol.Item().PaddingTop(8).AlignCenter().Text("MY ASHARA AZAM")
                                .FontFamily(Fonts.Verdana).FontSize(20).ExtraBold().FontColor("#004E50");

                                mainCol.Item().PaddingTop(6).AlignCenter().Text("Acknowledging the sincere Niyyat of")
                                .FontSize(11).Italic().FontColor("#666666");

                                // Name & ID
                                mainCol.Item().PaddingTop(12).AlignCenter().Text(entry.FullName.ToUpper())
                                .FontFamily(Fonts.Georgia).FontSize(20).Bold().FontColor("#C59F46");

                                mainCol.Item().AlignCenter().Text($"ITS ID: {entry.ITSNumber}")
                                .FontSize(13).Medium().FontColor("#004E50");

                                // Commitment Header (Tightened)
                                mainCol.Item().PaddingTop(2).AlignCenter()
                                .Text("Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein:")
                                .FontSize(14).SemiBold().FontColor("#004E50");

                                // Content List
                                mainCol.Item().PaddingTop(1).PaddingLeft(25).Column(listCol =>
                                {
                                    string[] items = {
                            "Maro Business 100% close raakhis.",
                            "Job Si Raza Lay-Lais.",
                            "Studies Si Raza Lay-Lais.",
                            "Qablal Waqt Waaz ni Majalis Ma Hazir Rahis."
                                };

                                    foreach (var item in items)
                                    {
                                        listCol.Item().PaddingBottom(1).Row(row =>
                                        {
                                            row.ConstantItem(20).Text("•").FontSize(16).FontColor("#C59F46");
                                            row.RelativeItem().PaddingTop(2).Text(item)
                                            .FontSize(12).FontColor("#333333").LineHeight(1.1f);
                                        });
                                    }
                                });
                            });

                            // --- FOOTER SECTION (Kept just above bottom vine) ---
                            column.Item().AlignBottom().PaddingBottom(53).AlignCenter().Column(ft =>
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
            return File(pdfBytes, "application/pdf", $"AsharaAzam_Certificate_{entry.ITSNumber}.pdf");
        }

        public async Task<IActionResult> GenerateAsharaAzam(string itsNumber)
        {
            var entry = await _context.AsharaAzamEntries
                .FirstOrDefaultAsync(e => e.ITSNumber == itsNumber);

            if (entry == null) return NotFound();

            return View("AsharaAzamVideo", entry);
        }
    }
}