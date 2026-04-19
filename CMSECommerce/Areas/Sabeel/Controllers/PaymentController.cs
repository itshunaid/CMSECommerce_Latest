using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Areas.Sabeel.Controllers
{
    [Area("Sabeel")]
    public class PaymentController : Controller
    {
        private readonly DataContext _context;

        public PaymentController(DataContext context)
        {
            _context = context;
        }

        // 1. LIST: View all contributions
        public async Task<IActionResult> Index()
        {
            // Sorted by newest so users see their latest submission at the top
            var payments = await _context.SabeelPaymentDetails
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(payments);
        }

        // 2. FORM: Display the initial form
        public IActionResult Create()
        {
            // Server-side mobile detection
            var isMobile = DetectMobileDevice(HttpContext.Request);
            ViewBag.IsMobileDevice = isMobile;
            ViewBag.DeviceType = isMobile ? "mobile" : "desktop";

            return View(new SabeelPaymentDetail());
        }

        // 3. POST: Handle the form submission and save to DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] SabeelPaymentDetail paymentDetail)
        {
            // Model validation
            if (!ModelState.IsValid) return View(paymentDetail);

            if (paymentDetail.ITSNumber?.Length != 8)
            {
                ModelState.AddModelError("ITSNumber", "ITS Number must be 8 digits.");
                return View(paymentDetail);
            }

            try
            {
                paymentDetail.CreatedAt = DateTime.Now;
                paymentDetail.IsVerified = false;

                _context.Add(paymentDetail);
                await _context.SaveChangesAsync();

                TempData["success"] = "Payment details submitted! Please wait for verification.";

                // After Create, we go to Index. 
                // From Index, the user can click 'Pay Now' if they haven't paid yet.
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Database error: " + ex.Message);
                return View(paymentDetail);
            }
        }

        // 4. DISPLAY QR: Dedicated page for scanning the QR code
        public async Task<IActionResult> PayNow(int id)
        {
            var payment = await _context.SabeelPaymentDetails.FindAsync(id);
            if (payment == null) return NotFound();

            // Pass data to the View to show the correct Amount and ITS in the QR
            return View(payment);
        }

        // 5. UPDATE UTR: If a user needs to update their UTR later
        [HttpPost]
        public async Task<IActionResult> UpdateUtr(int id, string utr)
        {
            var payment = await _context.SabeelPaymentDetails.FindAsync(id);
            if (payment != null && !string.IsNullOrWhiteSpace(utr))
            {
                payment.UTRNumber = utr;
                await _context.SaveChangesAsync();
                TempData["success"] = "UTR updated successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. ADMIN VERIFY: Toggle the IsVerified flag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var payment = await _context.SabeelPaymentDetails.FindAsync(id);
            if (payment != null)
            {
                payment.IsVerified = true;
                await _context.SaveChangesAsync();
                TempData["success"] = "Payment marked as Verified.";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Server-side mobile device detection using User-Agent header
        /// </summary>
        private bool DetectMobileDevice(HttpRequest request)
        {
            string userAgent = request.Headers["User-Agent"].ToString().ToLower();

            // List of mobile device identifiers
            string[] mobileIdentifiers = new[]
            {
                "mobile",
                "android",
                "iphone",
                "ipod",
                "ipad",
                "windows phone",
                "blackberry",
                "webos",
                "opera mini",
                "opera mobi",
                "playstation",
                "kindle",
                "nexus",
                "samsung",
                "motorola",
                "htc",
                "lg-",
                "sony",
                "oneplus",
                "nokia",
                "asus",
                "realme",
                "xiaomi",
                "poco",
                "redmi",
                "mi ",
                "vivo",
                "oppo",
                "nothing",
                "infinix",
                "tecno"
            };

            // Desktop identifiers to exclude false positives
            string[] desktopIdentifiers = new[]
            {
                "windows nt",
                "macintosh",
                "linux",
                "x11",
                "x64",
                "x86"
            };

            // Check if user agent contains desktop identifiers
            bool isDesktop = desktopIdentifiers.Any(identifier => userAgent.Contains(identifier));

            // If it's clearly a desktop, return false
            if (isDesktop && !userAgent.Contains("android"))
            {
                return false;
            }

            // Check if user agent contains mobile identifiers
            bool isMobile = mobileIdentifiers.Any(identifier => userAgent.Contains(identifier));

            return isMobile;
        }
    }
}