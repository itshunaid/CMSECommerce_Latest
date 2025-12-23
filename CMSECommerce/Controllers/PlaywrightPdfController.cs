using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Threading.Tasks;
using System;

namespace CMSECommerce.Controllers
{
 public class PlaywrightPdfController : Controller
 {
 private readonly DataContext _context;

 public PlaywrightPdfController(DataContext context)
 {
 _context = context;
 }

 [HttpGet]
 public async Task<IActionResult> InvoicePdf(int id)
 {
 try
 {
 var exists = await _context.Orders.AnyAsync(o => o.Id == id);
 if (!exists) return NotFound();

 // Build absolute URL for the invoice page
 var invoiceUrl = Url.Action("Invoice", "Orders", new { id }, Request.Scheme, Request.Host.Value);

 // Create playwright instance and ensure browsers are installed via InstallAsync on the BrowserFetcher if necessary
 using var playwright = await Playwright.CreateAsync();
 // Note: Playwright will auto-download browsers when you run 'playwright install' during deployment.

 await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Args = new[] { "--no-sandbox" } });
 var page = await browser.NewPageAsync();

 await page.GotoAsync(invoiceUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

 var pdfStream = await page.PdfAsync(new PagePdfOptions
 {
 Format = "A4",
 PrintBackground = true,
 Margin = new Margin { Top = "0.5in", Right = "0.5in", Bottom = "0.5in", Left = "0.5in" }
 });

 var filename = $"Invoice-{id}.pdf";
 return File(pdfStream.ToArray(), "application/pdf", filename);
 }
 catch (Exception ex)
 {
 TempData["Error"] = "Failed to generate PDF: " + ex.Message;
 return RedirectToAction("Invoice", "Orders", new { id });
 }
 }
 }
}
