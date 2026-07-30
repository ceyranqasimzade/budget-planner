using budget_planner.DAL;
using budget_planner.Extensions;
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
    public class HomeController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string GuestCardsKey = "Guest_Cards";
        private const string GuestTransactionsKey = "Guest_Transactions";

        private static readonly HashSet<string> VisibleCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "USD", "EUR", "TRY", "GBP", "RUB", "AED", "CHF", "CAD", "CNY", "GEL"
        };

        public HomeController(
            BudgetDbContext context,
            ICurrencyService currencyService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _currencyService = currencyService;
            _userManager = userManager;
        }

        // =========================================================================
        // 🟢 STATIC HELPERS
        // =========================================================================
        private static Dictionary<string, decimal> CreateRateDictionary(IEnumerable<CurrencyRateVM> rates)
        {
            return rates
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Rate,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static decimal ConvertToAzn(decimal amount, string? currency, Dictionary<string, decimal> rates)
        {
            if (string.IsNullOrWhiteSpace(currency) ||
                string.Equals(currency, "AZN", StringComparison.OrdinalIgnoreCase))
                return amount;

            if (rates.TryGetValue(currency, out var rate))
                return amount * rate;

            return amount;
        }

        private static decimal CalculateTrend(decimal current, decimal previous)
        {
            if (previous > 0)
                return Math.Round(((current - previous) / previous) * 100, 1);

            return current > 0 ? 100m : 0m;
        }

        private static void AddNotification(DashboardVM vm, string title, string message, string icon, string color)
        {
            vm.Notifications.Add(new NotificationVM
            {
                Title = title,
                Message = message,
                IconClass = icon,
                TextColorClass = color
            });
        }

        // =========================================================================
        // 🟢 ANA METHOD: Index()
        // =========================================================================
        public async Task<IActionResult> Index()
        {
            var vm = new DashboardVM
            {
                Cards = new List<CardVM>(),
                LastTransactions = new List<TransactionVM>(),
                CategoryExpenses = new List<CategoryExpenseVM>(),
                Notifications = new List<NotificationVM>(),
                UpcomingPayments = new List<SubscriptionVM>(),
                ActiveGoals = new List<GoalVM>(),
                BaseCurrencySymbol = "₼"
            };

            var rates = await _currencyService.GetExchangeRatesAsync();
            var ratesDict = CreateRateDictionary(rates);

            vm.ExchangeRates = rates
                .Where(x => !string.IsNullOrWhiteSpace(x.Code) && VisibleCurrencies.Contains(x.Code))
                .ToList();

            // -----------------------------------------------------------------
            // 🟢 ƏLAVƏ OLUNAN HİSSƏ: Ekranda fərqin dublikat olmamağı üçün
            // -----------------------------------------------------------------
            foreach (var rate in vm.ExchangeRates)
            {
                if (rate.PreviousRate == 0)
                {
                    rate.PreviousRate = rate.Rate;
                }
            }
            // -----------------------------------------------------------------

            vm.Categories = await _context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => new CategorySelectVM { Id = c.Id, Name = c.Name })
                .ToListAsync();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // ------------------------------------------
                // 1. QONAQ İSTİFADƏÇİ (SESSION)
                // ------------------------------------------
                var guestUpcoming = HttpContext.Session.GetObject<List<UpcomingPayment>>("Guest_UpcomingPayments")
                                     ?? new List<UpcomingPayment>();

                ViewBag.UpcomingPayments = guestUpcoming
                    .Where(p => !p.IsPaid)
                    .OrderBy(p => p.DueDate)
                    .Take(5)
                    .ToList();

                LoadGuestDashboard(vm, ratesDict);
                return View(vm);
            }
            else
            {
                // ------------------------------------------
                // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
                // ------------------------------------------
                var upcomingPayments = await _context.UpcomingPayments
                    .AsNoTracking()
                    .Where(u => u.UserId == user.Id && !u.IsPaid)
                    .OrderBy(u => u.DueDate)
                    .Take(5)
                    .ToListAsync();

                ViewBag.UpcomingPayments = upcomingPayments;

                await LoadUserCardsAndCashAsync(vm, user.Id, ratesDict);
                await LoadMonthlyStatisticsAsync(vm, user.Id, ratesDict);
                await LoadGoalsAndSubscriptionsAsync(vm, user.Id);

                return View(vm);
            }
        }

        // =========================================================================
        // 🟢 QONAQ (GUEST) DASHBOARD
        // =========================================================================
        private void LoadGuestDashboard(DashboardVM vm, Dictionary<string, decimal> ratesDict)
        {
            AddNotification(vm, "Qonaq Rejimi", "Siz hazırda sınaq rejimindəsiniz. Brauzer bağlandıqda və ya Session müddəti bitdikdə məlumatlar silinəcək.", "bi-exclamation-triangle-fill", "text-warning");

            var guestCards = HttpContext.Session.GetObject<List<Card>>(GuestCardsKey);
            var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>(GuestTransactionsKey) ?? new List<Transaction>();

            if (guestCards == null)
            {
                guestCards = new List<Card>();
                HttpContext.Session.SetObject(GuestCardsKey, guestCards);
            }

            // 1. Bank Kartları Siyahısı
            vm.Cards = guestCards.Select(c => new CardVM
            {
                Id = c.Id,
                CardName = c.CardName,
                Last4Digits = c.Last4Digits,
                Currency = c.Currency ?? "AZN",
                Balance = c.Balance
            }).ToList();

            // 2. Nağd Pul Balansının Hesablanması (CardId == null olan əməliyyatlar)
            var cashTransactions = guestTransactions
                .Where(t => t.CardId == null && !t.IsDeleted)
                .ToList();

            vm.CashBalance = cashTransactions.Sum(t =>
                t.IsIncome
                    ? ConvertToAzn(t.Amount, t.Currency, ratesDict)
                    : -ConvertToAzn(t.Amount, t.Currency, ratesDict));

            // 3. Ümumi Balans (Bank Kartları Balansı + Yaşıl Nağd Pul Balansı)
            var totalCardsBalanceInAZN = guestCards.Sum(c => ConvertToAzn(c.Balance, c.Currency, ratesDict));
            vm.TotalBalance = totalCardsBalanceInAZN + vm.CashBalance;

            // Kart adlarını O(1) sürətlə tapmaq üçün Dictionary
            var cardLookup = guestCards
                .GroupBy(c => c.Id)
                .ToDictionary(g => g.Key, g => g.First());

            // 4. Son Əməliyyatlar Siyahısı
            vm.LastTransactions = guestTransactions
                .Where(t => !t.IsDeleted)
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
                    CardName = t.CardId.HasValue && cardLookup.TryGetValue(t.CardId.Value, out var card) ? card.CardName : "Nağd Pul"
                }).ToList();

            // 5. Kateqoriya üzrə Xərclər
            vm.CategoryExpenses = guestTransactions
                .Where(t => !t.IsIncome && !t.IsDeleted)
                .GroupBy(t => t.Category != null ? t.Category.Name : "Ümumi")
                .Select(g => new CategoryExpenseVM
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(t => ConvertToAzn(t.Amount, t.Currency, ratesDict))
                }).ToList();

            // 6. Qonaq Rejimi üçün Cari Ayın Gəlir və Xərclərinin Hesablanması
            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;

            vm.TotalIncome = guestTransactions
                .Where(t => t.IsIncome && !t.IsDeleted && t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .Sum(t => ConvertToAzn(t.Amount, t.Currency, ratesDict));

            vm.TotalExpense = guestTransactions
                .Where(t => !t.IsIncome && !t.IsDeleted && t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .Sum(t => ConvertToAzn(t.Amount, t.Currency, ratesDict));
        }

        // =========================================================================
        // 🟢 İSTİFADƏÇİ (USER) KART VƏ NAĞD PUL HESABLARI
        // =========================================================================
        private async Task LoadUserCardsAndCashAsync(DashboardVM vm, string userId, Dictionary<string, decimal> ratesDict)
        {
            vm.Cards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Select(c => new CardVM
                {
                    Id = c.Id,
                    CardName = c.CardName,
                    Last4Digits = c.Last4Digits,
                    Currency = c.Currency ?? "AZN",
                    Balance = c.Balance
                })
                .ToListAsync();

            // Bütün transaksiyaları çəkmək əvəzinə birbaşa valyuta və mədaxil növünə görə qruplaşdırıb SQL-dən oxuyuruq
            var cashGroups = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.CardId == null && !t.IsDeleted)
                .GroupBy(t => new { t.IsIncome, t.Currency })
                .Select(g => new
                {
                    g.Key.IsIncome,
                    Currency = g.Key.Currency ?? "AZN",
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            vm.CashBalance = cashGroups.Sum(g =>
                g.IsIncome
                    ? ConvertToAzn(g.TotalAmount, g.Currency, ratesDict)
                    : -ConvertToAzn(g.TotalAmount, g.Currency, ratesDict));

            var totalCardsBalanceInAZN = vm.Cards.Sum(c => ConvertToAzn(c.Balance, c.Currency, ratesDict));
            vm.TotalBalance = totalCardsBalanceInAZN + vm.CashBalance;
        }

        // =========================================================================
        // 🟢 AYLIQ STATİSTİKALAR VƏ ANALİTİKA
        // =========================================================================
        private async Task LoadMonthlyStatisticsAsync(DashboardVM vm, string userId, Dictionary<string, decimal> ratesDict)
        {
            var today = DateTime.Today;
            var startOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var startOfNextMonth = startOfThisMonth.AddMonths(1);

            await LoadMonthlyTotalsAsync(vm, userId, startOfThisMonth, startOfNextMonth, ratesDict);
            await LoadLastTransactionsAsync(vm, userId);
            vm.CategoryExpenses = await GetUserCategoryExpensesAsync(userId, ratesDict);
            await LoadTrendsAsync(vm, userId, startOfThisMonth, ratesDict);
            LoadAdviceAndWarnings(vm);
        }

        private async Task LoadMonthlyTotalsAsync(DashboardVM vm, string userId, DateTime start, DateTime end, Dictionary<string, decimal> ratesDict)
        {
            // Bütün datanı yaddaşa çəkmədən SQL tərəfində qruplaşdırma
            var monthlyGroups = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Date >= start && t.Date < end && !t.IsDeleted)
                .GroupBy(t => new { t.IsIncome, t.Currency })
                .Select(g => new
                {
                    g.Key.IsIncome,
                    Currency = g.Key.Currency ?? "AZN",
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            decimal income = 0;
            decimal expense = 0;

            foreach (var g in monthlyGroups)
            {
                var amount = ConvertToAzn(g.TotalAmount, g.Currency, ratesDict);
                if (g.IsIncome)
                    income += amount;
                else
                    expense += amount;
            }

            vm.TotalIncome = income;
            vm.TotalExpense = expense;
        }

        private async Task LoadLastTransactionsAsync(DashboardVM vm, string userId)
        {
            vm.LastTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && !t.IsDeleted)
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
                    CardName = t.Card != null ? t.Card.CardName : "Nağd"
                })
                .ToListAsync();
        }

        private async Task LoadTrendsAsync(DashboardVM vm, string userId, DateTime startOfThisMonth, Dictionary<string, decimal> ratesDict)
        {
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            // SQL səviyyəsində valyuta və mədaxil növünə görə qruplaşdırma
            var lastMonthGroups = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Date >= startOfLastMonth && t.Date < startOfThisMonth && !t.IsDeleted)
                .GroupBy(t => new { t.IsIncome, t.Currency })
                .Select(g => new
                {
                    g.Key.IsIncome,
                    Currency = g.Key.Currency ?? "AZN",
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            decimal lastMonthIncome = 0;
            decimal lastMonthExpense = 0;

            foreach (var g in lastMonthGroups)
            {
                var amount = ConvertToAzn(g.TotalAmount, g.Currency, ratesDict);
                if (g.IsIncome)
                    lastMonthIncome += amount;
                else
                    lastMonthExpense += amount;
            }

            vm.IncomeTrend = CalculateTrend(vm.TotalIncome, lastMonthIncome);
            vm.ExpenseTrend = CalculateTrend(vm.TotalExpense, lastMonthExpense);
        }

        private void LoadAdviceAndWarnings(DashboardVM vm)
        {
            if (vm.TotalIncome > 0)
            {
                var savingsRate = ((vm.TotalIncome - vm.TotalExpense) / vm.TotalIncome) * 100;
                vm.FinancialAdvice = savingsRate > 0
                    ? $"Bu ay gəlirinizin {savingsRate:F0}%-ni qənaət etmisiniz. Mükəmməl göstəricidir!"
                    : "Bu ay xərcləriniz gəlirinizi üstələyir. Xərclərinizə diqqət etməyiniz tövsiyə olunur.";
            }

            if (vm.CategoryExpenses.Any() && vm.TotalExpense > 0)
            {
                var topCategory = vm.CategoryExpenses.MaxBy(c => c.Amount);
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
                AddNotification(vm, "Büdcə Xəbərdarlığı", vm.BudgetWarning, "bi-exclamation-triangle-fill", "text-warning");

            if (!string.IsNullOrEmpty(vm.FinancialAdvice))
                AddNotification(vm, "Maliyyə Məsləhəti", vm.FinancialAdvice, "bi-lightbulb-fill", "text-info");
        }

        // =========================================================================
        // 🟢 HƏDƏFLƏR VƏ ABUNƏLİKLƏR
        // =========================================================================
        private async Task LoadGoalsAndSubscriptionsAsync(DashboardVM vm, string userId)
        {
            var now = DateTime.UtcNow;

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .Select(s => new SubscriptionVM
                {
                    Name = s.Name,
                    Amount = s.Amount,
                    NextPaymentDate = s.NextPaymentDate,
                    IconClass = s.IconClass ?? "bi-credit-card",
                    ColorClass = s.ColorClass ?? "bg-primary"
                })
                .ToListAsync();

            var upcomingTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && !t.IsDeleted && (t.Date > now || t.Status == "Gözləmədə"))
                .OrderBy(t => t.Date)
                .Take(5)
                .Select(t => new SubscriptionVM
                {
                    Name = t.Description ?? "Gözləyən ödəniş",
                    Amount = t.Amount,
                    NextPaymentDate = t.Date,
                    IconClass = "bi-clock-history",
                    ColorClass = "bg-warning"
                })
                .ToListAsync();

            vm.UpcomingPayments = subscriptions
                .Concat(upcomingTransactions)
                .OrderBy(p => p.NextPaymentDate)
                .Take(5)
                .ToList();

            vm.ActiveGoals = await _context.Goals
                .AsNoTracking()
                .Where(g => g.UserId == userId && !g.IsDeleted)
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

        private async Task<List<CategoryExpenseVM>> GetUserCategoryExpensesAsync(string userId, Dictionary<string, decimal> ratesDict)
        {
            var expenseTransactionsForChart = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && !t.IsDeleted && !t.IsIncome && t.Category != null)
                .GroupBy(t => new { CategoryName = t.Category!.Name, t.Currency })
                .Select(g => new
                {
                    g.Key.CategoryName,
                    Currency = g.Key.Currency ?? "AZN",
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            return expenseTransactionsForChart
                .GroupBy(t => t.CategoryName)
                .Select(g => new CategoryExpenseVM
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(t => ConvertToAzn(t.TotalAmount, t.Currency, ratesDict))
                })
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenseChartData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { labels = Array.Empty<string>(), values = Array.Empty<decimal>() });
            }

            var rates = await _currencyService.GetExchangeRatesAsync();
            var ratesDict = CreateRateDictionary(rates);
            var categoryExpenses = await GetUserCategoryExpensesAsync(user.Id, ratesDict);

            var labels = categoryExpenses.Select(x => x.CategoryName).ToArray();
            var values = categoryExpenses.Select(x => x.Amount).ToArray();
            return Json(new { labels, values });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}