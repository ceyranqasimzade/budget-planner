using budget_planner.DAL;
using budget_planner.ViewModels;
using budget_planner.Services;
using budget_planner.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace budget_planner.Controllers
{
    public class HomeController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly CurrencyService _currencyService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            BudgetDbContext context,
            CurrencyService currencyService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _currencyService = currencyService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardVM();

            // Qonaqlar daxil olduqda səhifənin çökməməsi üçün siyahıları boş olaraq başladırıq.
            vm.Cards = new List<CardVM>();
            vm.LastTransactions = new List<TransactionVM>();
            vm.CategoryExpenses = new List<CategoryExpenseVM>();
            vm.Notifications = new List<NotificationVM>();
            vm.UpcomingPayments = new List<SubscriptionVM>();
            vm.ActiveGoals = new List<GoalVM>();

            // 🟢 1. VALYUTA VƏ KATEQORİYALAR (HƏM QONAQ, HƏM İSTİFADƏÇİ ÜÇÜN ÇƏKİLİR)
            vm.BaseCurrencySymbol = "₼";
            var rates = await _currencyService.GetExchangeRatesAsync();
            vm.ExchangeRates = rates;

            vm.Categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategorySelectVM
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            var user = await _userManager.GetUserAsync(User);

            // 🟢 2. ƏGƏR QONAQDIRSA (DAXİL OLMAMISA) XƏBƏRDARLIQ BİLDİRİŞİ GÖSTƏRİRİK
            if (user == null)
            {
                vm.Notifications.Add(new NotificationVM
                {
                    Title = "Qonaq Rejimi",
                    Message = "Siz hazırda sınaq rejimindəsiniz. Saytdan çıxdıqda və ya səhifəni yenilədikdə məlumatlar itəcək.",
                    IconClass = "bi-exclamation-triangle-fill",
                    TextColorClass = "text-warning"
                });
            }
            else
            {
                // Ödənilməmiş və vaxtı ən yaxın olan ilk 5 ödənişi çəkirik
                var upcomingPayments = await _context.UpcomingPayments
                    .Where(u => u.UserId == user.Id && !u.IsPaid)
                    .OrderBy(u => u.DueDate)
                    .Take(5)
                    .ToListAsync();

                ViewBag.UpcomingPayments = upcomingPayments;

                // DİNANİK ÇEVİRMƏ METODU: İstənilən məbləğ və valyutanı AZN-ə çevirir
                decimal ConvertToAzn(decimal amount, string? currency)
                {
                    if (string.IsNullOrWhiteSpace(currency) || currency.Equals("AZN", StringComparison.OrdinalIgnoreCase))
                        return amount;

                    var rateObj = rates?.FirstOrDefault(r => r.Code.Equals(currency, StringComparison.OrdinalIgnoreCase));
                    decimal rate = rateObj != null ? rateObj.Rate : 1.0m;

                    return amount * rate;
                }

                // 1. İstifadəçinin kartlarını gətiririk
                vm.Cards = await _context.Cards
                    .Where(c => c.UserId == user.Id && !c.IsDeleted)
                    .Select(c => new CardVM
                    {
                        Id = c.Id,
                        CardName = c.CardName,
                        Last4Digits = c.Last4Digits,
                        Currency = c.Currency ?? "AZN",
                        Balance = c.Balance
                    })
                    .ToListAsync();

                // 2. Nağd pul balansını hesablayırıq
                var cashTransactions = await _context.Transactions
                    .Where(t => t.UserId == user.Id && t.CardId == null && !t.IsDeleted)
                    .ToListAsync();

                var cashIncome = cashTransactions
                    .Where(t => t.IsIncome)
                    .Sum(t => ConvertToAzn(t.Amount, t.Currency));

                var cashExpense = cashTransactions
                    .Where(t => !t.IsIncome)
                    .Sum(t => ConvertToAzn(t.Amount, t.Currency));

                vm.CashBalance = cashIncome - cashExpense;

                // 3. Kart balanslarının cəmi
                decimal totalBalanceInAZN = 0;

                foreach (var card in vm.Cards)
                {
                    if (card.Currency == "AZN")
                    {
                        totalBalanceInAZN += card.Balance;
                    }
                    else
                    {
                        var rateObj = rates?.FirstOrDefault(r => r.Code.Equals(card.Currency, StringComparison.OrdinalIgnoreCase));
                        decimal rate = rateObj != null ? rateObj.Rate : 1.0m;
                        totalBalanceInAZN += card.Balance * rate;
                    }
                }

                vm.TotalBalance = totalBalanceInAZN + vm.CashBalance;

                // 4. Bu ayın gəlir və xərcləri
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var thisMonthTransactions = await _context.Transactions
                    .Where(t => t.UserId == user.Id && t.Date.Month == currentMonth && t.Date.Year == currentYear && !t.IsDeleted)
                    .ToListAsync();

                vm.TotalIncome = thisMonthTransactions
                    .Where(t => t.IsIncome)
                    .Sum(t => ConvertToAzn(t.Amount, t.Currency));

                vm.TotalExpense = thisMonthTransactions
                    .Where(t => !t.IsIncome)
                    .Sum(t => ConvertToAzn(t.Amount, t.Currency));

                // 5. Son 5 əməliyyatı çəkirik
                vm.LastTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Card)
                    .Where(t => t.UserId == user.Id && !t.IsDeleted)
                    .OrderByDescending(t => t.Date)
                    .Take(5)
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
                        CardName = t.Card != null ? t.Card.CardName : null
                    })
                    .ToListAsync();

                // 6. Qrafik üçün xərclərin kateqoriya üzrə AZN bölgüsü
                var expenseTransactionsForChart = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.UserId == user.Id && !t.IsDeleted && !t.IsIncome && t.Category != null)
                    .ToListAsync();

                vm.CategoryExpenses = expenseTransactionsForChart
                    .GroupBy(t => t.Category.Name)
                    .Select(g => new CategoryExpenseVM
                    {
                        CategoryName = g.Key,
                        Amount = g.Sum(t => ConvertToAzn(t.Amount, t.Currency))
                    })
                    .ToList();

                // 7. Trend hesablamaları
                var firstDayOfThisMonth = new DateTime(currentYear, currentMonth, 1);
                var firstDayOfLastMonth = firstDayOfThisMonth.AddMonths(-1);

                var lastMonthTransactions = await _context.Transactions
                    .Where(t => t.UserId == user.Id && t.Date >= firstDayOfLastMonth && t.Date < firstDayOfThisMonth && !t.IsDeleted)
                    .ToListAsync();

                var lastMonthIncome = lastMonthTransactions.Where(t => t.IsIncome).Sum(t => ConvertToAzn(t.Amount, t.Currency));
                var lastMonthExpense = lastMonthTransactions.Where(t => !t.IsIncome).Sum(t => ConvertToAzn(t.Amount, t.Currency));

                if (lastMonthIncome > 0)
                    vm.IncomeTrend = ((vm.TotalIncome - lastMonthIncome) / lastMonthIncome) * 100;
                else if (vm.TotalIncome > 0)
                    vm.IncomeTrend = 100;
                else
                    vm.IncomeTrend = 0;

                if (lastMonthExpense > 0)
                    vm.ExpenseTrend = ((vm.TotalExpense - lastMonthExpense) / lastMonthExpense) * 100;
                else if (vm.TotalExpense > 0)
                    vm.ExpenseTrend = 100;
                else
                    vm.ExpenseTrend = 0;

                // 8. Maliyyə məsləhəti
                if (vm.TotalIncome > 0)
                {
                    var netSavings = vm.TotalIncome - vm.TotalExpense;
                    var savingsRate = (netSavings / vm.TotalIncome) * 100;

                    if (savingsRate > 0)
                    {
                        vm.FinancialAdvice = $"Bu ay gəlirinizin {savingsRate:F0}%-ni qənaət etmisiniz. Mükəmməl göstəricidir!";
                    }
                    else
                    {
                        vm.FinancialAdvice = "Bu ay xərcləriniz gəlirinizi üstələyir. Xərclərinizə diqqət etməyiniz tövsiyə olunur.";
                    }
                }

                if (vm.CategoryExpenses != null && vm.CategoryExpenses.Any() && vm.TotalExpense > 0)
                {
                    var topCategory = vm.CategoryExpenses.OrderByDescending(c => c.Amount).FirstOrDefault();
                    if (topCategory != null)
                    {
                        var percentOfTotal = (topCategory.Amount / vm.TotalExpense) * 100;
                        if (percentOfTotal >= 40)
                        {
                            vm.BudgetWarning = $"\"{topCategory.CategoryName}\" kateqoriyası ümumi xərclərinizin {percentOfTotal:F0}%-ni təşkil edir. Limitinizə diqqət edin!";
                        }
                    }
                }

                if (!string.IsNullOrEmpty(vm.BudgetWarning))
                {
                    vm.Notifications.Add(new NotificationVM
                    {
                        Title = "Büdcə Xəbərdarlığı",
                        Message = vm.BudgetWarning,
                        IconClass = "bi-exclamation-triangle-fill",
                        TextColorClass = "text-warning"
                    });
                }

                if (!string.IsNullOrEmpty(vm.FinancialAdvice))
                {
                    vm.Notifications.Add(new NotificationVM
                    {
                        Title = "Maliyyə Məsləhəti",
                        Message = vm.FinancialAdvice,
                        IconClass = "bi-lightbulb-fill",
                        TextColorClass = "text-info"
                    });
                }

                // 9. Qarşıdan gələn ödənişlər və Hədəflər
                vm.UpcomingPayments = await _context.Subscriptions
                    .Where(s => s.UserId == user.Id && !s.IsDeleted)
                    .OrderBy(s => s.NextPaymentDate)
                    .Take(3)
                    .Select(s => new SubscriptionVM
                    {
                        Name = s.Name,
                        Amount = s.Amount,
                        NextPaymentDate = s.NextPaymentDate,
                        IconClass = s.IconClass ?? "bi-credit-card",
                        ColorClass = s.ColorClass ?? "bg-primary"
                    })
                    .ToListAsync();

                vm.ActiveGoals = await _context.Goals
                    .Where(g => g.UserId == user.Id && !g.IsDeleted)
                    .OrderBy(g => g.Deadline)
                    .Take(3)
                    .Select(g => new GoalVM
                    {
                        Name = g.Title,
                        TargetAmount = g.TargetAmount,
                        CurrentAmount = g.CurrentAmount,
                        IconClass = "bi-star-fill",
                        ColorClass = "bg-info"
                    })
                    .ToListAsync();
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenseChartData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { labels = new string[0], values = new decimal[0] });
            }
            var rates = await _currencyService.GetExchangeRatesAsync();

            decimal ConvertToAzn(decimal amount, string? currency)
            {
                if (string.IsNullOrWhiteSpace(currency) || currency.Equals("AZN", StringComparison.OrdinalIgnoreCase))
                    return amount;
                var rateObj = rates?.FirstOrDefault(r => r.Code.Equals(currency, StringComparison.OrdinalIgnoreCase));
                decimal rate = rateObj != null ? rateObj.Rate : 1.0m;
                return amount * rate;
            }

            var categoryExpensesRaw = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == user.Id && !t.IsDeleted && !t.IsIncome && t.Category != null)
                .ToListAsync();

            var categoryExpenses = categoryExpensesRaw
                .GroupBy(t => t.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(t => ConvertToAzn(t.Amount, t.Currency))
                })
                .ToList();

            var labels = categoryExpenses.Select(x => x.CategoryName).ToArray();
            var values = categoryExpenses.Select(x => x.Amount).ToArray();
            return Json(new { labels, values });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction(TransactionCreateVM model)
        {
            var user = await _userManager.GetUserAsync(User);

            // 🟢 QONAQ İSTİFADƏÇİ ƏMƏLİYYAT SINAYAN ZAMAN LOGIN-Ə YÖNLƏNDİRMİRİK
            if (user == null)
            {
                TempData["SuccessMessage"] = "Əməliyyat sınaq rejimində icra olundu! Saytdan çıxdıqda və ya səhifəni yenilədikdə məlumatlar sıfırlanacaq.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Zəhmət olmasa formadakı məlumatları düzgün doldurun.";
                return RedirectToAction(nameof(Index));
            }

            int categoryId = 0;

            if (!string.IsNullOrWhiteSpace(model.NewCategoryName))
            {
                var trimmedName = model.NewCategoryName.Trim();
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower() && !c.IsDeleted);

                if (existingCategory != null)
                {
                    categoryId = existingCategory.Id;
                }
                else
                {
                    var newCategory = new Category
                    {
                        Name = trimmedName,
                        Type = model.IsIncome ? "Income" : "Expense"
                    };
                    _context.Categories.Add(newCategory);
                    await _context.SaveChangesAsync();

                    categoryId = newCategory.Id;
                }
            }
            else if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == model.CategoryId.Value && !c.IsDeleted);
                if (!categoryExists)
                {
                    TempData["ErrorMessage"] = "Seçilmiş kateqoriya bazada tapılmadı.";
                    return RedirectToAction(nameof(Index));
                }
                categoryId = model.CategoryId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(model.CategoryName))
            {
                var trimmedName = model.CategoryName.Trim();
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower() && !c.IsDeleted);

                if (category != null)
                {
                    categoryId = category.Id;
                }
                else
                {
                    var newCategory = new Category
                    {
                        Name = trimmedName,
                        Type = model.IsIncome ? "Income" : "Expense"
                    };
                    _context.Categories.Add(newCategory);
                    await _context.SaveChangesAsync();

                    categoryId = newCategory.Id;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Zəhmət olmasa bir kateqoriya seçin və ya yeni kateqoriya daxil edin.";
                return RedirectToAction(nameof(Index));
            }

            string selectedCurrency = !string.IsNullOrWhiteSpace(model.Currency) ? model.Currency : "AZN";
            var transaction = new Transaction
            {
                UserId = user.Id,
                Amount = model.Amount,
                Description = model.Description,
                IsIncome = model.IsIncome,
                CardId = model.CardId,
                CategoryId = categoryId,
                Currency = selectedCurrency,
                Date = model.Date != default ? model.Date : DateTime.Now
            };

            if (model.CardId.HasValue)
            {
                var card = await _context.Cards
                    .FirstOrDefaultAsync(c => c.Id == model.CardId.Value && c.UserId == user.Id && !c.IsDeleted);

                if (card != null)
                {
                    var rates = await _currencyService.GetExchangeRatesAsync();

                    decimal fromRate = selectedCurrency.Equals("AZN", StringComparison.OrdinalIgnoreCase)
                        ? 1.0m
                        : rates?.FirstOrDefault(r => r.Code.Equals(selectedCurrency, StringComparison.OrdinalIgnoreCase))?.Rate ?? 1.0m;
                    decimal toRate = (card.Currency ?? "AZN").Equals("AZN", StringComparison.OrdinalIgnoreCase)
                        ? 1.0m
                        : rates?.FirstOrDefault(r => r.Code.Equals(card.Currency, StringComparison.OrdinalIgnoreCase))?.Rate ?? 1.0m;

                    decimal convertedAmount = model.Amount * (fromRate / toRate);

                    if (model.IsIncome)
                    {
                        card.Balance += convertedAmount;
                    }
                    else
                    {
                        if (card.Balance < convertedAmount)
                        {
                            TempData["ErrorMessage"] = "Kartda kifayət qədər vəsait yoxdur.";
                            return RedirectToAction(nameof(Index));
                        }
                        card.Balance -= convertedAmount;
                    }
                }
            }
            else
            {
                if (!model.IsIncome)
                {
                    var rates = await _currencyService.GetExchangeRatesAsync();
                    decimal ConvertToAzn(decimal amount, string? currency)
                    {
                        if (string.IsNullOrWhiteSpace(currency) || currency.Equals("AZN", StringComparison.OrdinalIgnoreCase))
                            return amount;
                        var rateObj = rates?.FirstOrDefault(r => r.Code.Equals(currency, StringComparison.OrdinalIgnoreCase));
                        return amount * (rateObj != null ? rateObj.Rate : 1.0m);
                    }

                    var cashTransactions = await _context.Transactions
                        .Where(t => t.UserId == user.Id && t.CardId == null && !t.IsDeleted)
                        .ToListAsync();
                    decimal cashIncome = cashTransactions
                        .Where(t => t.IsIncome)
                        .Sum(t => ConvertToAzn(t.Amount, t.Currency));
                    decimal cashExpense = cashTransactions
                        .Where(t => !t.IsIncome)
                        .Sum(t => ConvertToAzn(t.Amount, t.Currency));
                    decimal currentCashBalance = cashIncome - cashExpense;
                    decimal newExpenseInAzn = ConvertToAzn(model.Amount, selectedCurrency);

                    if (currentCashBalance < newExpenseInAzn)
                    {
                        TempData["ErrorMessage"] = $"Nağd balansı kifayət etmir! Mövcud Nağd Balansınız: {currentCashBalance:N2} AZN";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Əməliyyat uğurla əlavə edildi!";
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}