using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace budget_planner.Controllers
{
    public class GoalController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<GoalController> _logger;

        public GoalController(
            BudgetDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrencyService currencyService,
            ILogger<GoalController> logger)
        {
            _context = context;
            _userManager = userManager;
            _currencyService = currencyService;
            _logger = logger;
        }

        // Bütün məqsədlərin siyahısı
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                ViewBag.Cards = new List<Card>();
                return View(new List<GoalVM>());
            }

            var goalsVM = await _context.Goals
                .Where(g => g.UserId == user.Id && !g.IsDeleted)
                .OrderByDescending(g => g.Id)
                .Select(g => new GoalVM
                {
                    Id = g.Id,
                    Name = g.Title,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    IconClass = g.IconClass ?? "bi-flag-fill",
                    ColorClass = g.ColorClass ?? "bg-primary"
                })
                .ToListAsync();

            ViewBag.Cards = await _context.Cards
                .Where(c => c.UserId == user.Id && !c.IsDeleted)
                .ToListAsync();

            return View(goalsVM);
        }

        // Yeni Məqsəd Yaradılması
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GoalVM model)
        {
            // 🟢 Early Exit - ModelState yoxlanışı ən əvvələ çəkildi
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Məlumatları düzgün daxil etdiyinizdən əmin olun.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["SuccessMessage"] = "Hədəf sınaq rejimində əlavə edildi! Məlumatlar yadda saxlanılmayacaq.";
                return RedirectToAction(nameof(Index));
            }

            var goal = new Goal
            {
                Title = model.Name,
                TargetAmount = model.TargetAmount,
                CurrentAmount = model.CurrentAmount,
                IconClass = string.IsNullOrWhiteSpace(model.IconClass) ? "bi-flag-fill" : model.IconClass,
                ColorClass = string.IsNullOrWhiteSpace(model.ColorClass) ? "bg-primary" : model.ColorClass,
                UserId = user.Id,
                IsDeleted = false
            };

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yeni Hədəf uğurla əlavə edildi!";
            return RedirectToAction(nameof(Index));
        }

        // Məqsədə Pul Əlavə Edilməsi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMoney(int goalId, decimal amount, string paymentMethod)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Məbləğ sıfırdan böyük olmalıdır!";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["SuccessMessage"] = "Əməliyyat sınaq rejimində icra edildi!";
                return RedirectToAction(nameof(Index));
            }

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == user.Id && !g.IsDeleted);

            if (goal == null)
            {
                TempData["ErrorMessage"] = "Hədəf tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            Card? selectedCard = null;
            decimal amountDeductedFromSource = amount; // AZN ilə
            string sourceCurrency = "AZN";

            // --- 1. BALANS YOXLANIŞI VƏ VALYUTA KONVERTASİYASI ---
            if (paymentMethod != "cash" && int.TryParse(paymentMethod, out int selectedCardId))
            {
                selectedCard = await _context.Cards
                    .FirstOrDefaultAsync(c => c.Id == selectedCardId && c.UserId == user.Id && !c.IsDeleted);

                if (selectedCard == null)
                {
                    TempData["ErrorMessage"] = "Seçilmiş kart tapılmadı!";
                    return RedirectToAction(nameof(Index));
                }

                sourceCurrency = !string.IsNullOrWhiteSpace(selectedCard.Currency)
                    ? selectedCard.Currency.Trim().ToUpper()
                    : "AZN";

                amountDeductedFromSource = await _currencyService.ConvertAsync(amount, "AZN", sourceCurrency);

                if (selectedCard.Balance < amountDeductedFromSource)
                {
                    TempData["ErrorMessage"] = $"Kartda kifayət qədər vəsait yoxdur! (Çıxılacaq məbləğ: {amountDeductedFromSource:N2} {sourceCurrency})";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (paymentMethod == "cash")
            {
                if (user.CashBalance < amount)
                {
                    TempData["ErrorMessage"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Mövcud nağd: {user.CashBalance:N2} AZN)";
                    return RedirectToAction(nameof(Index));
                }
            }

            // --- 2. ATOMIC TRANSACTION ---
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                goal.CurrentAmount += amount;

                if (selectedCard != null)
                {
                    selectedCard.Balance -= amountDeductedFromSource;
                }
                else if (paymentMethod == "cash")
                {
                    user.CashBalance -= amount;
                }

                user.TotalBalance -= amount;

                // 🟢 Bütün dəyişikliklər tək bir SaveChangesAsync() ilə vahid DbContext üzərindən bazaya yazılır
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"{amount:N2} AZN məbləği Hədəfinizə uğurla əlavə edildi!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Hədəfə pul əlavə edərkən xəta baş verdi. GoalId: {GoalId}, UserId: {UserId}", goalId, user.Id);
                TempData["ErrorMessage"] = "Əməliyyat zamanı gözlənilməz xəta baş verdi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Məqsədin Silinməsi (Soft Delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["SuccessMessage"] = "Hədəf sınaq rejimində silindi!";
                return RedirectToAction(nameof(Index));
            }

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == user.Id && !g.IsDeleted);

            if (goal != null)
            {
                goal.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hədəf uğurla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Hədəf tapılmadı və ya artıq silinib.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}