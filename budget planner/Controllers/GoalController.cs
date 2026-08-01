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

        // --- 1. BÜTÜN HƏDƏFLƏRİN SİYAHISI (TAM TƏMİZLƏNMİŞ) ---
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // 1. QONAQ İSTİFADƏÇİ (Daxil olmayanlar)
            if (user == null)
            {
                ViewBag.Cards = new List<Card>();

                // Cümlələri JS idarə edəcək deyə, bura sadəcə boş siyahı göndəririk ki, səhifə 500 xətası verməsin
                return View(new GoalsVM
                {
                    Goals = new List<GoalVM>()
                });
            }

            // 2. DAXİL OLAN İSTİFADƏÇİ
            var goalsList = await _context.Goals
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

            var viewModel = new GoalsVM
            {
                Goals = goalsList
            };

            return View(viewModel);
        }

        // --- 2. YENİ HƏDƏF YARADILMASI ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GoalVM model)
        {
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

        // --- 3. HƏDƏFƏ PUL ƏLAVƏ EDİLMƏSİ ---
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

            Card selectedCard = null;
            decimal amountDeductedFromSource = amount; // AZN ilə
            string sourceCurrency = "AZN";

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

        // --- 4. HƏDƏFİN SİLİNMƏSİ ---
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