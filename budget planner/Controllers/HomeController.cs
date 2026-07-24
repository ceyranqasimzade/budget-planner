using budget_planner.DAL;
using budget_planner.ViewModels;
using budget_planner.Services;
using budget_planner.Models; // ApplicationUser modelinizin olduğu yer
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace budget_planner.Controllers
{
    // [Authorize] silindi - Ana səhifə hər kəsə açıqdır
    public class HomeController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly CurrencyService _currencyService;
        private readonly UserManager<ApplicationUser> _userManager; // AppUser əvəzinə ApplicationUser yazıldı

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

            // 0. Daxil olan istifadəçini yoxlayırıq
            var user = await _userManager.GetUserAsync(User);

            // Əgər istifadəçi sistemə daxil olubsa, onun şəxsi məlumatlarını çəkirik
            if (user != null)
            {
                // 1. Yalnız daxil olan istifadəçinin silinməmiş kartlarını çəkirik
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

                // 2. Nağd pul balansını hesablayırıq (CardId == null olanlar)
                var cashIncome = await _context.Transactions
                    .Where(t => t.UserId == user.Id && t.CardId == null && t.IsIncome && !t.IsDeleted)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var cashExpense = await _context.Transactions
                    .Where(t => t.UserId == user.Id && t.CardId == null && !t.IsIncome && !t.IsDeleted)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                vm.CashBalance = cashIncome - cashExpense;

                // 3. Ümumi balansı hesablayırıq (Kartlar + Nağd Pul)
                vm.TotalBalance = vm.Cards.Sum(c => c.Balance) + vm.CashBalance;

                // 4. Bu ayın gəlir və xərclərini hesablayırıq
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var thisMonthTransactions = _context.Transactions
                    .Where(t => t.UserId == user.Id && t.Date.Month == currentMonth && t.Date.Year == currentYear && !t.IsDeleted);

                vm.TotalIncome = await thisMonthTransactions
                    .Where(t => t.IsIncome)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                vm.TotalExpense = await thisMonthTransactions
                    .Where(t => !t.IsIncome)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                // 5. Son əməliyyatları çəkirik (Son 5 əməliyyat)
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
                        Currency = t.Currency ?? "₼",
                        CardId = t.CardId,
                        CardName = t.Card != null ? t.Card.CardName : null
                    })
                    .ToListAsync();

                // 7. Qrafik (Chart.js) üçün xərclərin kateqoriyalara görə bölgüsü
                vm.CategoryExpenses = await _context.Transactions
                    .Where(t => t.UserId == user.Id && !t.IsDeleted && !t.IsIncome && t.Category != null)
                    .GroupBy(t => t.Category.Name)
                    .Select(g => new CategoryExpenseVM
                    {
                        CategoryName = g.Key,
                        Amount = g.Sum(t => t.Amount)
                    })
                    .ToListAsync();

                // --- YENİ ƏLAVƏ EDİLƏN HİSSƏ: TRENDLƏR, MƏSLƏHƏTLƏR, BİLDİRİŞLƏR ---

                // 8. TREND (FAİZ) HESABLAMALARI
                var firstDayOfThisMonth = new DateTime(currentYear, currentMonth, 1);
                var firstDayOfLastMonth = firstDayOfThisMonth.AddMonths(-1);

                var lastMonthTransactions = _context.Transactions
                    .Where(t => t.UserId == user.Id && t.Date >= firstDayOfLastMonth && t.Date < firstDayOfThisMonth && !t.IsDeleted);

                var lastMonthIncome = await lastMonthTransactions
                    .Where(t => t.IsIncome)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var lastMonthExpense = await lastMonthTransactions
                    .Where(t => !t.IsIncome)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                if (lastMonthIncome > 0)
                {
                    vm.IncomeTrend = ((vm.TotalIncome - lastMonthIncome) / lastMonthIncome) * 100;
                }

                if (lastMonthExpense > 0)
                {
                    vm.ExpenseTrend = ((vm.TotalExpense - lastMonthExpense) / lastMonthExpense) * 100;
                }

                // 9. DİNAMİK MALİYYƏ MƏSLƏHƏTİ
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

                // 10. DİNAMİK BÜDCƏ XƏBƏRDARLIĞI
                if (vm.CategoryExpenses != null && vm.CategoryExpenses.Any() && vm.TotalExpense > 0)
                {
                    var topCategory = vm.CategoryExpenses.OrderByDescending(c => c.Amount).FirstOrDefault();
                    if (topCategory != null)
                    {
                        var percentOfTotal = (topCategory.Amount / vm.TotalExpense) * 100;
                        if (percentOfTotal >= 40) // Ümumi xərclərin 40%-ni keçirsə xəbərdarlıq edir
                        {
                            vm.BudgetWarning = $"\"{topCategory.CategoryName}\" kateqoriyası ümumi xərclərinizin {percentOfTotal:F0}%-ni təşkil edir. Limitinizə diqqət edin!";
                        }
                    }
                }

                // 11. DİNAMİK BİLDİRİŞLƏR (Zəng ikonu üçün)
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

                // 12. QARŞIDAN GƏLƏN ÖDƏNİŞLƏRİ ÇƏKMƏK
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

                // 13. HƏDƏFLƏRİ ÇƏKMƏK (Sizin Goal modelinizə uyğunlaşdırıldı)
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

            // 6. Gəlir/Xərc modalları üçün kateqoriyalar siyahısını çəkirik (Qonaqlar üçün də işləyir)
            vm.Categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategorySelectVM
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            // 8. Valyuta və Simvol parametrləri
            vm.BaseCurrencySymbol = "₼";
            vm.ExchangeRates = await _currencyService.GetExchangeRatesAsync();

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}