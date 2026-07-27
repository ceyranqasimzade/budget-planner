using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace budget_planner.Controllers
{
    public class TransactionController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CurrencyService _currencyService; // Canlı CBAR məzənnə servisi

        public TransactionController(
            BudgetDbContext context,
            UserManager<ApplicationUser> userManager,
            CurrencyService currencyService)
        {
            _context = context;
            _userManager = userManager;
            _currencyService = currencyService;
        }

        // GET: /Transaction/Index (Bütün əməliyyatların siyahısı)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // 🟢 1-Cİ HƏLL: Login səhifəsinə atmaq əvəzinə qonaqlar üçün boş siyahı qaytarırıq
            if (user == null)
            {
                return View(new List<TransactionVM>());
            }

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Card)
                .Where(t => t.UserId == user.Id && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .Select(t => new TransactionVM
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description,
                    Date = t.Date,
                    IsIncome = t.IsIncome,
                    CategoryName = t.Category != null ? t.Category.Name : "Ümumi",
                    Currency = t.Currency ?? "AZN",
                    CardId = t.CardId,
                    CardName = t.Card != null ? t.Card.CardName : "Nağd",
                    Status = t.Status
                })
                .ToListAsync();

            return View(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TransactionCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);

            // 🟢 2-Cİ HƏLL: Loginə atmaq əvəzinə qonaq istifadəçi üçün sınaq rejimini aktiv edirik
            if (user == null)
            {
                TempData["SuccessMessage"] = "Əməliyyat sınaq rejimində qeydə alındı! Saytdan çıxdıqda və ya səhifəni yenilədikdə məlumatlar sıfırlanacaq.";
                return RedirectToAction("Index", "Home");
            }

            // Formadan gələn valyutanı götürürük (boşdursa AZN istifadə olunur)
            string transactionCurrency = !string.IsNullOrWhiteSpace(model.Currency) ? model.Currency : "AZN";
            string currency = transactionCurrency;

            // 1. KART BALANSININ YENİLƏNMƏSİ (Əgər kart seçilibsə)
            if (model.CardId.HasValue && model.CardId.Value > 0)
            {
                var card = await _context.Cards.FindAsync(model.CardId);

                if (card == null || card.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Seçilmiş kart tapılmadı və ya silinib!";
                    return RedirectToAction("Index", "Home");
                }

                // --- CANLI MƏZƏNNƏ İLƏ KONVERTASİYA MƏNTİQİ ---
                decimal rate = await GetExchangeRateAsync(transactionCurrency, card.Currency);
                decimal convertedAmount = model.Amount * rate;

                if (model.IsIncome)
                {
                    card.Balance += convertedAmount;
                }
                else
                {
                    if (card.Balance < convertedAmount)
                    {
                        TempData["ErrorMessage"] = $"Kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {convertedAmount:N2} {card.Currency})";
                        return RedirectToAction("Index", "Home");
                    }
                    card.Balance -= convertedAmount;
                }

                currency = transactionCurrency; // Əməliyyatın öz valyutası bazaya yazılır
            }

            // 2. KATEQORİYA MƏNTİQİ (Siyahıdan seçilibsə / yeni yazılıbsa / boşdursa)
            int categoryId;
            if (!string.IsNullOrWhiteSpace(model.CategoryName))
            {
                var categoryNameClean = model.CategoryName.Trim();
                var category = _context.Categories
                    .FirstOrDefault(c => c.Name.ToLower() == categoryNameClean.ToLower());

                // Əgər yazılan kateqoriya bazada yoxdursa, yeni yaradılır
                if (category == null)
                {
                    category = new Category
                    {
                        Name = categoryNameClean,
                        Type = model.IsIncome ? "Gəlir" : "Xərc"
                    };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                categoryId = category.Id;
            }
            else
            {
                // Kateqoriya boş buraxılıbsa "Ümumi" kateqoriyası istifadə olunur
                var defaultCategory = _context.Categories.FirstOrDefault(c => c.Name == "Ümumi");
                if (defaultCategory == null)
                {
                    defaultCategory = new Category { Name = "Ümumi", Type = "Müxtəlif" };
                    _context.Categories.Add(defaultCategory);
                    await _context.SaveChangesAsync();
                }

                categoryId = defaultCategory.Id;
            }

            // 3. ƏMƏLİYYATIN BAZAYA YAZILMASI
            var transaction = new Transaction
            {
                CardId = model.CardId, // Nağd olduqda null olaraq yazılır
                Amount = model.Amount,
                Description = model.Description,
                IsIncome = model.IsIncome,
                Date = model.Date != default ? model.Date : DateTime.Now,
                Currency = currency,
                UserId = user.Id,
                CategoryId = categoryId,
                Status = "Tamamlandı"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Əməliyyat uğurla qeydə alındı!";
            return RedirectToAction("Index", "Home");
        }

        // --- CANLI CBAR MƏZƏNNƏSİNİ İSTİFADƏ EDƏN KÖMƏKÇİ FUNKSİYA ---
        private async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            if (fromCurrency == toCurrency) return 1.0m;

            // CurrencyService-dən canlı məzənnələri çəkirik
            var rates = await _currencyService.GetExchangeRatesAsync();

            // CBAR məzənnələri AZN-ə nəzərən olduğu üçün AZN məzənnəsini 1.0 götürürük
            decimal fromRateInAzn = fromCurrency == "AZN"
                ? 1.0m
                : rates.FirstOrDefault(r => r.Code == fromCurrency)?.Rate ?? 1.0m;

            decimal toRateInAzn = toCurrency == "AZN"
                ? 1.0m
                : rates.FirstOrDefault(r => r.Code == toCurrency)?.Rate ?? 1.0m;

            // Nisbəti hesablayırıq
            return fromRateInAzn / toRateInAzn;
        }
    }
}