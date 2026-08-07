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
        private const string GuestGoalsKey = "Guest_Goals";
        private const string GuestUpcomingKey = "Guest_UpcomingPayments";
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
                .GroupBy(x => x.Code!, StringComparer.OrdinalIgnoreCase)
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
        private static decimal ConvertCurrency(decimal amount, string? fromCurrency, string? toCurrency, Dictionary<string, decimal> rates)
        {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
                return amount;
            decimal amountInAzn = ConvertToAzn(amount, fromCurrency, rates);
            if (string.IsNullOrWhiteSpace(toCurrency) || string.Equals(toCurrency, "AZN", StringComparison.OrdinalIgnoreCase))
                return amountInAzn;
            if (rates.TryGetValue(toCurrency, out var toRate) && toRate > 0)
                return amountInAzn / toRate;
            return amountInAzn;
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
            foreach (var rate in vm.ExchangeRates)
            {
                if (rate.PreviousRate == 0)
                {
                    rate.PreviousRate = rate.Rate;
                }
            }
            vm.Categories = await _context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => new CategorySelectVM { Id = c.Id, Name = c.Name ?? string.Empty })
                .ToListAsync();
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                var guestUpcoming = HttpContext.Session.GetObject<List<UpcomingPayment>>(GuestUpcomingKey)
                                     ?? HttpContext.Session.GetObject<List<UpcomingPayment>>("Guest_UpcomingPayments")
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
            vm.Cards = guestCards.Select(c => new CardVM
            {
                Id = c.Id,
                CardName = c.CardName ?? string.Empty,
                Last4Digits = c.Last4Digits ?? string.Empty,
                Currency = c.Currency ?? "AZN",
                Balance = c.Balance
            }).ToList();
            var cashTransactions = guestTransactions
                .Where(t => t.CardId == null && !t.IsDeleted)
                .ToList();
            vm.CashBalance = cashTransactions.Sum(t =>
                t.IsIncome
                    ? ConvertToAzn(t.Amount, t.Currency, ratesDict)
                    : -ConvertToAzn(t.Amount, t.Currency, ratesDict));
            var totalCardsBalanceInAZN = guestCards.Sum(c => ConvertToAzn(c.Balance, c.Currency, ratesDict));
            vm.TotalBalance = totalCardsBalanceInAZN + vm.CashBalance;
            var cardLookup = guestCards
                .GroupBy(c => c.Id)
                .ToDictionary(g => g.Key, g => g.First());
            vm.LastTransactions = guestTransactions
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .Select(t => new TransactionVM
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description ?? string.Empty,
                    Date = t.Date,
                    IsIncome = t.IsIncome,
                    CategoryName = t.Category != null ? t.Category.Name : "Ümumi",
                    Currency = t.Currency ?? "AZN",
                    CardId = t.CardId,
                    CardName = t.CardId.HasValue && cardLookup.TryGetValue(t.CardId.Value, out var card) ? card.CardName : "Nağd Pul"
                }).ToList();
            vm.CategoryExpenses = guestTransactions
                .Where(t => !t.IsIncome && !t.IsDeleted)
                .GroupBy(t => t.Category != null ? t.Category.Name : "Ümumi")
                .Select(g => new CategoryExpenseVM
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(t => ConvertToAzn(t.Amount, t.Currency, ratesDict))
                }).ToList();
            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;
            vm.TotalIncome = guestTransactions
                .Where(t => t.IsIncome && !t.IsDeleted && t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .Sum(t => ConvertToAzn(t.Amount, t.Currency, ratesDict));
            vm.TotalExpense = guestTransactions
                .Where(t => !t.IsIncome && !t.IsDeleted && t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .Sum(t => ConvertToAzn(t.Amount, t.Currency, ratesDict));
            var guestGoals = HttpContext.Session.GetObject<List<Goal>>(GuestGoalsKey) ?? new List<Goal>();
            vm.ActiveGoals = guestGoals
                .Take(3)
                .Select(g => new GoalVM
                {
                    Id = g.Id,
                    Name = g.Name ?? string.Empty,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    Currency = g.Currency ?? "AZN",
                    IconClass = "bi-star-fill",
                    ColorClass = "bg-info"
                }).ToList();
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
                    CardName = c.CardName ?? string.Empty,
                    Last4Digits = c.Last4Digits ?? string.Empty,
                    Currency = c.Currency ?? "AZN",
                    Balance = c.Balance
                })
                .ToListAsync();
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
                    Description = t.Description ?? string.Empty,
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
            if (vm.CashBalance < 0)
            {
                vm.BudgetWarning = $"Nağd pul balansınız mənfidir ({vm.CashBalance:N2} {vm.BaseCurrencySymbol})! Xərcləmək üçün kifayət qədər nağd vesaitiniz yoxdur.";
            }
            else if (vm.TotalBalance < 0)
            {
                vm.BudgetWarning = $"Ümumi balansınız mənfidir ({vm.TotalBalance:N2} {vm.BaseCurrencySymbol}).";
            }
            else if (vm.TotalExpense > vm.TotalIncome)
            {
                vm.BudgetWarning = $"Xərcləriniz ({vm.TotalExpense:N2} {vm.BaseCurrencySymbol}) gəlirlərinizdən ({vm.TotalIncome:N2} {vm.BaseCurrencySymbol}) çoxdur.";
            }
            else if (vm.CategoryExpenses.Any() && vm.TotalExpense > 0)
            {
                var topCategory = vm.CategoryExpenses.MaxBy(c => c.Amount);
                if (topCategory != null)
                {
                    var percentOfTotal = (topCategory.Amount / vm.TotalExpense) * 100;
                    if (percentOfTotal >= 40)
                    {
                        vm.BudgetWarning = $"\"{topCategory.CategoryName}\" kateqoriyası ümumi xərclərinizin {percentOfTotal:F0}%-ni təşkil edir.";
                    }
                }
            }
            if (!string.IsNullOrEmpty(vm.BudgetWarning))
            {
                AddNotification(
                    vm,
                    "Büdcə Xəbərdarlığı",
                    vm.BudgetWarning,
                    "bi-exclamation-triangle-fill",
                    "text-danger");
            }
            if (!string.IsNullOrEmpty(vm.FinancialAdvice))
            {
                AddNotification(
                    vm,
                    "Maliyyə Məsləhəti",
                    vm.FinancialAdvice,
                    "bi-lightbulb-fill",
                    "text-info");
            }
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
                    Name = s.Name ?? string.Empty,
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
                    Id = g.Id,
                    Name = g.Name ?? string.Empty,
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
                    CategoryName = g.Key ?? "Ümumi",
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
        // =========================================================================
        // 🟢 HƏDƏFƏ PUL ƏLAVƏ ETMƏ
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> DepositToGoal(int goalId, decimal amount, int? cardId)
        {
            if (amount <= 0) return BadRequest("Məbləğ düzgün daxil edilməlidir.");
            var rates = await _currencyService.GetExchangeRatesAsync();
            var ratesDict = CreateRateDictionary(rates);
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == user.Id && !g.IsDeleted);
                    if (goal == null) return NotFound("Hədəf tapılmadı.");
                    string sourceCurrency = "AZN";
                    if (cardId.HasValue)
                    {
                        var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == cardId.Value && c.UserId == user.Id && !c.IsDeleted);
                        if (card == null) return NotFound("Bank kartı tapılmadı.");

                        if (card.Balance < amount) return BadRequest("Kartda kifayət qədər balans yoxdur.");

                        card.Balance -= amount;
                        sourceCurrency = card.Currency ?? "AZN";
                    }
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = user.Id,
                        Amount = amount,
                        IsIncome = false,
                        Currency = sourceCurrency,
                        CardId = cardId,
                        Description = $"Hədəfə köçürmə: {goal.Name}",
                        Date = DateTime.Now
                    });

                    decimal convertedAmount = ConvertCurrency(amount, sourceCurrency, goal.Currency ?? "AZN", ratesDict);
                    goal.CurrentAmount += convertedAmount;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Xəta baş verdi, köçürmə tamamlanmadı.");
                }
            }
            else
            {
                var guestGoals = HttpContext.Session.GetObject<List<Goal>>(GuestGoalsKey) ?? new List<Goal>();
                var goal = guestGoals.FirstOrDefault(g => g.Id == goalId && !g.IsDeleted);
                if (goal == null) return NotFound("Hədəf tapılmadı.");
                string sourceCurrency = "AZN";
                if (cardId.HasValue)
                {
                    var guestCards = HttpContext.Session.GetObject<List<Card>>(GuestCardsKey) ?? new List<Card>();
                    var card = guestCards.FirstOrDefault(c => c.Id == cardId.Value && !c.IsDeleted);
                    if (card == null || card.Balance < amount) return BadRequest("Balans kifayət etmir.");
                    card.Balance -= amount;
                    sourceCurrency = card.Currency ?? "AZN";
                    HttpContext.Session.SetObject(GuestCardsKey, guestCards);
                }
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>(GuestTransactionsKey) ?? new List<Transaction>();
                int newId = guestTransactions.Count > 0 ? guestTransactions.Max(t => t.Id) + 1 : 1;
                guestTransactions.Add(new Transaction
                {
                    Id = newId,
                    Amount = amount,
                    IsIncome = false,
                    Currency = sourceCurrency,
                    CardId = cardId,
                    Description = $"Hədəfə köçürmə: {goal.Name}",
                    Date = DateTime.Now
                });
                HttpContext.Session.SetObject(GuestTransactionsKey, guestTransactions);
                decimal convertedAmount = ConvertCurrency(amount, sourceCurrency, goal.Currency ?? "AZN", ratesDict);
                goal.CurrentAmount += convertedAmount;
                HttpContext.Session.SetObject(GuestGoalsKey, guestGoals);
            }
            return RedirectToAction(nameof(Index));
        }
        // =========================================================================
        // 🟢 GÖZLƏYƏN ÖDƏNİŞİ İCRA ETMƏ
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayUpcomingPayment(int id, int? cardId = null)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var payment = await _context.UpcomingPayments
                        .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id && !p.IsPaid);
                    if (payment == null) return NotFound("Gözləyən ödəniş tapılmadı.");
                    string sourceCurrency = "AZN";
                    if (cardId.HasValue)
                    {
                        var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == cardId.Value && c.UserId == user.Id && !c.IsDeleted);
                        if (card == null) return NotFound("Bank kartı tapılmadı.");

                        if (card.Balance < payment.Amount) return BadRequest("Kartda kifayət qədər balans yoxdur.");

                        card.Balance -= payment.Amount;
                        sourceCurrency = card.Currency ?? "AZN";
                    }
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = user.Id,
                        Amount = payment.Amount,
                        IsIncome = false,
                        Currency = sourceCurrency,
                        CardId = cardId,
                        Description = $"{payment.Title} ödənişi edildi",
                        Date = DateTime.Now
                    });

                    if (payment.IsRecurring)
                    {
                        payment.DueDate = payment.DueDate.AddMonths(1);
                        _context.UpcomingPayments.Update(payment);
                    }
                    else
                    {
                        payment.IsPaid = true;
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Xəta baş verdi, ödəniş icra olunmadı.");
                }
            }
            else
            {
                var guestUpcoming = HttpContext.Session.GetObject<List<UpcomingPayment>>(GuestUpcomingKey)
                                     ?? HttpContext.Session.GetObject<List<UpcomingPayment>>("Guest_UpcomingPayments")
                                     ?? new List<UpcomingPayment>();
                var payment = guestUpcoming.FirstOrDefault(p => p.Id == id && !p.IsPaid);
                if (payment == null) return NotFound("Gözləyən ödəniş tapılmadı.");
                string sourceCurrency = "AZN";
                if (cardId.HasValue)
                {
                    var guestCards = HttpContext.Session.GetObject<List<Card>>(GuestCardsKey) ?? new List<Card>();
                    var card = guestCards.FirstOrDefault(c => c.Id == cardId.Value && !c.IsDeleted);
                    if (card == null || card.Balance < payment.Amount) return BadRequest("Balans kifayət etmir.");

                    card.Balance -= payment.Amount;
                    sourceCurrency = card.Currency ?? "AZN";
                    HttpContext.Session.SetObject(GuestCardsKey, guestCards);
                }
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>(GuestTransactionsKey) ?? new List<Transaction>();
                int newId = guestTransactions.Count > 0 ? guestTransactions.Max(t => t.Id) + 1 : 1;
                guestTransactions.Add(new Transaction
                {
                    Id = newId,
                    Amount = payment.Amount,
                    IsIncome = false,
                    Currency = sourceCurrency,
                    CardId = cardId,
                    Description = $"{payment.Title} ödənişi edildi",
                    Date = DateTime.Now
                });
                HttpContext.Session.SetObject(GuestTransactionsKey, guestTransactions);
                if (payment.IsRecurring)
                {
                    payment.DueDate = payment.DueDate.AddMonths(1);
                }
                else
                {
                    payment.IsPaid = true;
                }
                HttpContext.Session.SetObject(GuestUpcomingKey, guestUpcoming);
                HttpContext.Session.SetObject("Guest_UpcomingPayments", guestUpcoming);
            }
            return RedirectToAction(nameof(Index));
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}