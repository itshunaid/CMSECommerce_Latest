using Microsoft.AspNetCore.Mvc.RazorPages;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CMSECommerce.Areas.Admin.Pages
{
 [Authorize(Roles = "Admin")]
 public class UserStatusSettingsModel : PageModel
 {
 private readonly DataContext _context;
 public UserStatusSetting Setting { get; set; }

 [BindProperty]
 public int OnlineThresholdMinutes { get; set; }
 [BindProperty]
 public int CleanupThresholdMinutes { get; set; }

 public UserStatusSettingsModel(DataContext context)
 {
 _context = context;
 }

 public async Task<IActionResult> OnGetAsync()
 {
 Setting = await _context.UserStatusSettings.FirstOrDefaultAsync();
 if (Setting == null)
 {
 // initialize with defaults
 Setting = new UserStatusSetting { OnlineThresholdMinutes =5, CleanupThresholdMinutes =60 };
 _context.UserStatusSettings.Add(Setting);
 await _context.SaveChangesAsync();
 }
 OnlineThresholdMinutes = Setting.OnlineThresholdMinutes;
 CleanupThresholdMinutes = Setting.CleanupThresholdMinutes;
 return Page();
 }

 public async Task<IActionResult> OnPostAsync()
 {
 Setting = await _context.UserStatusSettings.FirstOrDefaultAsync();
 if (Setting == null)
 {
 Setting = new UserStatusSetting();
 _context.UserStatusSettings.Add(Setting);
 }
 Setting.OnlineThresholdMinutes = OnlineThresholdMinutes;
 Setting.CleanupThresholdMinutes = CleanupThresholdMinutes;
 await _context.SaveChangesAsync();
 TempData["Success"] = "User status settings updated.";
 return RedirectToPage();
 }
 }
}
