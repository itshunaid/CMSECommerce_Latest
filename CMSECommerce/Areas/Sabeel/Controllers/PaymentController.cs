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

            // Server-side duplicate UTR check (final protection)
            if (!string.IsNullOrWhiteSpace(paymentDetail.UTRNumber))
            {
                var utrTrim = paymentDetail.UTRNumber.Trim();
                bool exists = await _context.SabeelPaymentDetails.AnyAsync(p => p.UTRNumber == utrTrim);
                if (exists)
                {
                    ModelState.AddModelError("UTRNumber", "⚠️ This UTR Number already exists in the system. Please verify your UTR.");
                    return View(paymentDetail);
                }
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

        // GET: Sabeel/Payment/GetExistingUtrs
        // Returns a JSON array of existing UTR numbers (non-null, non-empty)
        [HttpGet]
        public async Task<JsonResult> GetExistingUtrs()
        {
            try
            {
                var utrs = await _context.SabeelPaymentDetails
                    .AsNoTracking()
                    .Where(p => !string.IsNullOrEmpty(p.UTRNumber))
                    .Select(p => p.UTRNumber)
                    .ToListAsync();

                return Json(new { success = true, utrs });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetExistingUtrs error: {ex.Message}");
                return Json(new { success = false, message = ex.Message, utrs = Array.Empty<string>() });
            }
        }

        // GET: Sabeel/Payment/CheckDuplicateUTR?utrNumber=123456789012
        [HttpGet]
        public async Task<JsonResult> CheckDuplicateUTR(string utrNumber)
        {
            System.Diagnostics.Debug.WriteLine($"CheckDuplicateUTR called with: '{utrNumber}'");

            if (string.IsNullOrWhiteSpace(utrNumber) || utrNumber.Length != 12 || !utrNumber.All(char.IsDigit))
            {
                return Json(new { isDuplicate = false, message = "UTR must be 12 digits" });
            }

            try
            {
                bool exists = await _context.SabeelPaymentDetails
                    .AnyAsync(p => p.UTRNumber == utrNumber.Trim());

                System.Diagnostics.Debug.WriteLine($"CheckDuplicateUTR result for {utrNumber}: {exists}");
                return Json(new { isDuplicate = exists });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckDuplicateUTR error: {ex.Message}");
                return Json(new { isDuplicate = false, message = ex.Message });
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