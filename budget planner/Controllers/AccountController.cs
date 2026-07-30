using budget_planner.Models;
using budget_planner.ViewModels;
using budget_planner.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace budget_planner.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        // ==========================================
        // QEYDİYYAT (REGISTER)
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Təhlükəsizlik üçün əlavə edildi
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ==========================================
        // GİRİŞ (LOGIN)
        // ==========================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Təhlükəsizlik üçün əlavə edildi
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            ApplicationUser? user = null;

            // Null reference xəbərdarlığının qarşısını almaq üçün yoxlama
            if (!string.IsNullOrEmpty(model.UsernameOrEmail) && model.UsernameOrEmail.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(model.UsernameOrEmail);
            }
            else if (!string.IsNullOrEmpty(model.UsernameOrEmail))
            {
                user = await _userManager.FindByNameAsync(model.UsernameOrEmail);
            }

            // user və user.UserName üçün null yoxlamaları əlavə edildi
            if (user != null && !string.IsNullOrEmpty(user.UserName))
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "İstifadəçi adı/E-poçt və ya şifrə yanlışdır!");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // ==========================================
        // ÇIXIŞ (LOGOUT)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // ŞİFRƏNİ UNUTMUSAN
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Məsləhətdir ki, bura da əlavə olunsun
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { email = model.Email, token = token }, Request.Scheme);

            string emailBody = $"<h3>Şifrəni Sıfırlama</h3><p>Şifrənizi sıfırlamaq üçün <a href='{callbackUrl}'>BURAYA KLİKLƏYİN</a>.</p>";
            await _emailService.SendEmailAsync(model.Email, "Şifrənin Sıfırlanması", emailBody);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ==========================================
        // YENİ ŞİFRƏ TƏYİN ET
        // ==========================================
        [HttpGet]
        public IActionResult ResetPassword(string? email, string? token)
        {
            // string.IsNullOrEmpty istifadə olunaraq nullable dəyərlər təhlükəsiz idarə olunur
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return BadRequest("Keçərsiz sorğu.");

            var model = new ResetPasswordVM { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Məsləhətdir ki, bura da əlavə olunsun
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}