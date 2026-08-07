using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace budget_planner.Controllers
{
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAvatarService _avatarService;
        private readonly IUserDataCleanupService _cleanupService;
        private const string GUEST_SETTINGS_KEY = "Guest_Settings_Session";
        private readonly string[] _allowedCurrencies = { "AZN", "USD", "EUR", "TRY", "GBP", "RUB", "GEL", "AED", "CHF", "CNY", "CAD" };
        private readonly string[] _allowedLanguages = { "AZ", "EN", "TR", "RU" };
        public SettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAvatarService avatarService,
            IUserDataCleanupService cleanupService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _avatarService = avatarService;
            _cleanupService = cleanupService;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new SettingsVM();

            if (_signInManager.IsSignedIn(User))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.FullName = user.FullName ?? string.Empty;
                    model.Email = user.Email ?? string.Empty;
                    model.DefaultCurrency = user.DefaultCurrency ?? "AZN";
                    model.Language = user.Language ?? "AZ";
                    model.BudgetAlerts = user.BudgetAlerts;
                    model.EmailNotifications = user.EmailNotifications;
                    model.ProfilePicturePath = string.IsNullOrEmpty(user.ProfilePicturePath)
                        ? "/images/default-avatar.png" : user.ProfilePicturePath;
                }
            }
            else
            {
                var sessionData = HttpContext.Session.GetString(GUEST_SETTINGS_KEY);
                if (!string.IsNullOrEmpty(sessionData))
                {
                    try { model = JsonSerializer.Deserialize<SettingsVM>(sessionData) ?? new SettingsVM(); }
                    catch { model = new SettingsVM(); }
                }
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(SettingsVM model, IFormFile? AvatarFile)
        {
            bool isAuthenticated = _signInManager.IsSignedIn(User);
            if (!string.IsNullOrEmpty(model.DefaultCurrency) && !_allowedCurrencies.Contains(model.DefaultCurrency))
                ModelState.AddModelError(nameof(model.DefaultCurrency), "Yanlış valyuta seçildi.");
            if (!string.IsNullOrEmpty(model.Language) && !_allowedLanguages.Contains(model.Language))
                ModelState.AddModelError(nameof(model.Language), "Yanlış dil seçildi.");
            if (AvatarFile != null && AvatarFile.Length > 0 && !await _avatarService.IsValidImageAsync(AvatarFile))
                ModelState.AddModelError("AvatarFile", "Fayl zədəlidir, saxtadır və ya icazə verilməyən formatdadır.");
            if (!ModelState.IsValid) return View("Index", model);
            if (isAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    user.ProfilePicturePath = await _avatarService.UploadAvatarAsync(AvatarFile, user.ProfilePicturePath);
                }
                if (!string.IsNullOrWhiteSpace(model.FullName)) user.FullName = model.FullName.Trim();
                // Əgər null gələrsə default dəyərlər atanır
                user.DefaultCurrency = model.DefaultCurrency ?? "AZN";
                user.Language = model.Language ?? "AZ";
                user.BudgetAlerts = model.BudgetAlerts;
                user.EmailNotifications = model.EmailNotifications;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    TempData["Error"] = "Parametrləri yadda saxlayarkən xəta baş verdi.";
                    return RedirectToAction(nameof(Index));
                }
                await _signInManager.RefreshSignInAsync(user);
            }
            else
            {
                HttpContext.Session.SetString(GUEST_SETTINGS_KEY, JsonSerializer.Serialize(model));
            }
            TempData["Success"] = isAuthenticated ? "Parametrlər hesabınıza yadda saxlanıldı." : "Parametrlər sessiya üçün yadda saxlanıldı.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetData()
        {
            if (_signInManager.IsSignedIn(User))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _cleanupService.DeleteFinancialDataAsync(user.Id);
                }
            }
            else
            {
                HttpContext.Session.Remove(GUEST_SETTINGS_KEY);
            }
            TempData["Success"] = "Hesabınız aktiv qalmaqla, bütün maliyyə məlumatlarınız uğurla silindi.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            if (!_signInManager.IsSignedIn(User)) return RedirectToAction(nameof(Index));
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));
            var avatarPath = user.ProfilePicturePath;
            var result = await _cleanupService.DeleteUserWithDataTransactionAsync(user, _userManager);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Hesabı silərkən xəta baş verdi.";
                return RedirectToAction(nameof(Index));
            }
            _avatarService.DeleteAvatar(avatarPath);
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Hesabınız və bütün maliyyə məlumatlarınız həmişəlik silindi.";
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            if (string.IsNullOrEmpty(culture)) culture = "AZ";

            if (!_allowedLanguages.Contains(culture.ToUpper())) culture = "AZ";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Null yoxlanışı və local url kontrolu
            var safeUrl = (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl)) ? "/" : returnUrl;
            return LocalRedirect(safeUrl);
        }
    }
}