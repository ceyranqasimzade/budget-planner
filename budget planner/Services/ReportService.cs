using budget_planner.DAL;
using budget_planner.Extensions; // SessionExtensions üçün
using budget_planner.Services.ReportModels;
using budget_planner.ViewModels.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace budget_planner.Services
{
    public class ReportService : IReportService
    {
        private readonly BudgetDbContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly string[] MonthNamesAz =
            { "Yan", "Fev", "Mar", "Apr", "May", "İyn", "İyl", "Avq", "Sen", "Okt", "Noy", "Dek" };

        private static readonly string[] DayNamesAz =
            { "Bazar e.", "Çərşənbə a.", "Çərşənbə", "Cümə a.", "Cümə", "Şənbə", "Bazar" };

        public ReportService(
            BudgetDbContext context,
            ICurrencyService currencyService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _currencyService = currencyService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ReportVM> GetReportDataAsync(string userId, ReportFilterVM filter = null)
        {
            filter ??= new ReportFilterVM();
            var displayCurrency = string.IsNullOrWhiteSpace(filter.DisplayCurrency) ? "AZN" : filter.DisplayCurrency;

            var now = DateTime.Now;
            var currentMonthStart = filter.StartDate ?? new DateTime(now.Year, now.Month, 1);
            var currentMonthEnd = filter.EndDate ?? currentMonthStart.AddMonths(1).AddDays(-1);

            List<Models.Transaction> rawTransactions;

            // Əgər İstifadəçi Qonaqdırsa:
            if (userId != null && userId.StartsWith("guest_"))
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                string sessionKey = $"Guest_Transactions_{userId}";

                // Sizin yaratdığınız GetObject<T> genişlənmə metodundan istifadə olunur
                var guestTxs = session?.GetObject<List<Models.Transaction>>(sessionKey) ?? new List<Models.Transaction>();

                var guestQuery = guestTxs.Where(t => !t.IsDeleted);

                if (filter.CardId.HasValue)
                    guestQuery = guestQuery.Where(t => t.CardId == filter.CardId.Value);
                if (filter.CategoryId.HasValue)
                    guestQuery = guestQuery.Where(t => t.CategoryId == filter.CategoryId.Value);

                rawTransactions = guestQuery.ToList();
            }
            else
            {
                // Qeydiyyatlı istifadəçidirsə bazadan oxuyuruq
                var query = _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.UserId == userId && !t.IsDeleted);

                if (filter.CardId.HasValue) query = query.Where(t => t.CardId == filter.CardId.Value);
                if (filter.CategoryId.HasValue) query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

                rawTransactions = await query.ToListAsync();
            }

            // Normalizasiya və Hesabatın qurulması
            var rates = await GetCurrencyRatesAsync(displayCurrency, rawTransactions.Select(t => t.Currency).Distinct());
            var normalizedTxs = NormalizeTransactions(rawTransactions, rates, displayCurrency);

            return new ReportVM
            {
                Filter = filter,
                Kpi = BuildKpi(normalizedTxs, currentMonthStart, currentMonthEnd),
                Trend = BuildTrend(normalizedTxs, currentMonthStart),
                Categories = BuildCategories(normalizedTxs, currentMonthStart, currentMonthEnd),
                Weekdays = BuildWeekdays(normalizedTxs, currentMonthStart, currentMonthEnd)
            };
        }

        // =========================================================================
        // PRIVATE BUILDER METODLARI
        // =========================================================================

        private async Task<Dictionary<string, decimal>> GetCurrencyRatesAsync(string baseCurrency, IEnumerable<string> currencies)
        {
            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { baseCurrency, 1.0m }
            };

            foreach (var curr in currencies)
            {
                if (!rates.ContainsKey(curr))
                {
                    decimal rate = await _currencyService.ConvertAsync(1m, curr, baseCurrency);
                    rates[curr] = rate;
                }
            }
            return rates;
        }

        private List<ReportTransaction> NormalizeTransactions(
            List<Models.Transaction> rawTxs,
            Dictionary<string, decimal> rates,
            string baseCurrency)
        {
            return rawTxs.Select(t =>
            {
                decimal rate = string.Equals(t.Currency, baseCurrency, StringComparison.OrdinalIgnoreCase)
                    ? 1.0m
                    : rates.GetValueOrDefault(t.Currency, 1.0m);

                return new ReportTransaction
                {
                    Date = t.Date,
                    Amount = t.Amount * rate,
                    IsIncome = t.IsIncome,
                    CategoryName = t.Category?.Name ?? "Digər",
                    CardId = t.CardId
                };
            }).ToList();
        }

        private ReportKpiVM BuildKpi(List<ReportTransaction> txs, DateTime monthStart, DateTime monthEnd)
        {
            var currentTxs = txs.Where(t => t.Date >= monthStart && t.Date <= monthEnd).ToList();

            var prevMonthStart = monthStart.AddMonths(-1);
            var prevMonthEnd = monthStart.AddDays(-1);
            var prevTxs = txs.Where(t => t.Date >= prevMonthStart && t.Date <= prevMonthEnd).ToList();

            decimal income = currentTxs.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal expense = currentTxs.Where(t => !t.IsIncome).Sum(t => t.Amount);

            decimal prevIncome = prevTxs.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal prevExpense = prevTxs.Where(t => !t.IsIncome).Sum(t => t.Amount);

            return new ReportKpiVM
            {
                MonthlyIncome = income,
                MonthlyExpense = expense,
                MonthlySavings = income - expense,
                IncomeChangePercent = prevIncome > 0 ? ((income - prevIncome) / prevIncome) * 100 : 0,
                ExpenseChangePercent = prevExpense > 0 ? ((expense - prevExpense) / prevExpense) * 100 : 0,
                SavingsChangePercent = (prevIncome - prevExpense) != 0 ? (((income - expense) - (prevIncome - prevExpense)) / Math.Abs(prevIncome - prevExpense)) * 100 : 0,
                HealthScore = CalculateHealthScore(income, expense)
            };
        }

        private TrendChartVM BuildTrend(List<ReportTransaction> txs, DateTime currentMonthStart)
        {
            var grouped = txs
                .GroupBy(t => (t.Date.Year, t.Date.Month))
                .ToDictionary(
                    g => g.Key,
                    g => new { Income = g.Where(x => x.IsIncome).Sum(x => x.Amount), Expense = g.Where(x => !x.IsIncome).Sum(x => x.Amount) }
                );

            var trendVM = new TrendChartVM();

            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = currentMonthStart.AddMonths(-i);
                var key = (targetMonth.Year, targetMonth.Month);

                grouped.TryGetValue(key, out var data);

                trendVM.Points.Add(new TrendPointVM
                {
                    Label = $"{MonthNamesAz[targetMonth.Month - 1]} {targetMonth.Year}",
                    Income = data?.Income ?? 0,
                    Expense = data?.Expense ?? 0
                });
            }

            return trendVM;
        }

        private CategoryChartVM BuildCategories(List<ReportTransaction> txs, DateTime monthStart, DateTime monthEnd)
        {
            var catGrouped = txs
                .Where(t => t.Date >= monthStart && t.Date <= monthEnd && !t.IsIncome)
                .GroupBy(t => t.CategoryName)
                .Select(g => new { CategoryName = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            decimal totalExpense = catGrouped.Sum(x => x.Total);

            var vm = new CategoryChartVM
            {
                CategoryNames = catGrouped.Select(x => x.CategoryName).ToList(),
                CategoryExpenses = catGrouped.Select(x => x.Total).ToList()
            };

            foreach (var item in catGrouped.Take(5))
            {
                vm.TopCategories.Add(new TopCategoryVM
                {
                    CategoryName = item.CategoryName,
                    Amount = item.Total,
                    Percentage = totalExpense > 0 ? (double)(item.Total / totalExpense * 100) : 0
                });
            }

            return vm;
        }

        private WeekdayChartVM BuildWeekdays(List<ReportTransaction> txs, DateTime monthStart, DateTime monthEnd)
        {
            var dayArray = new decimal[7];
            var currentMonthExpenses = txs.Where(t => t.Date >= monthStart && t.Date <= monthEnd && !t.IsIncome);

            foreach (var tx in currentMonthExpenses)
            {
                int dayIndex = ((int)tx.Date.DayOfWeek + 6) % 7;
                dayArray[dayIndex] += tx.Amount;
            }

            return new WeekdayChartVM
            {
                DayNames = DayNamesAz.ToList(),
                DayExpenses = dayArray.ToList()
            };
        }

        private int CalculateHealthScore(decimal income, decimal expense)
        {
            if (income <= 0) return 20;

            int score = 15;
            if (income >= expense) score += 15;

            decimal savingsRatio = (income - expense) / income;
            if (savingsRatio > 0)
            {
                decimal cappedRatio = Math.Min(savingsRatio, 0.40m);
                score += (int)((cappedRatio / 0.40m) * 35m);
            }

            decimal expenseRatio = expense / income;
            if (expenseRatio < 1.0m)
            {
                score += (int)((1.0m - expenseRatio) * 35m);
            }

            return Math.Clamp(score, 0, 100);
        }
    }
}