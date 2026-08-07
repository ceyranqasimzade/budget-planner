using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.ViewModels.Reports;

namespace budget_planner.Services
{
    public class ReportService : IReportService
    {
        private readonly BudgetDbContext _context;
        private readonly ICurrencyService _currencyService;

        public ReportService(BudgetDbContext context, ICurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public async Task<ReportVM> GetReportDataAsync(string userId, ReportFilterVM? filter = null)
        {
            var filterObj = filter ?? new ReportFilterVM();
            var now = DateTime.Now;

            string targetCurrency = string.IsNullOrWhiteSpace(filterObj.DisplayCurrency)
                ? "AZN"
                : filterObj.DisplayCurrency.ToUpper();

            // 1. Bazadan Əsas Sorğu
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => !t.IsDeleted && t.UserId == userId)
                .AsQueryable();

            if (filterObj.StartDate.HasValue)
            {
                var start = filterObj.StartDate.Value.Date;
                query = query.Where(t => t.Date >= start);
            }

            if (filterObj.EndDate.HasValue)
            {
                var end = filterObj.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.Date <= end);
            }

            if (filterObj.CardId.HasValue && filterObj.CardId.Value > 0)
                query = query.Where(t => t.CardId == filterObj.CardId.Value);

            if (filterObj.CategoryId.HasValue && filterObj.CategoryId.Value > 0)
                query = query.Where(t => t.CategoryId == filterObj.CategoryId.Value);

            var transactions = await query.ToListAsync();

            // 2. Məbləğləri TargetCurrency-ə çevirmək (Optimizasiya olunmuş)
            var convertedTransactions = new List<ConvertedTransaction>();
            foreach (var tx in transactions)
            {
                string fromCurrency = string.IsNullOrEmpty(tx.Currency) ? "AZN" : tx.Currency.ToUpper();

                // Valyutalar eynidirsə API-yə sorğu göndərməyə ehtiyac yoxdur
                decimal convertedAmount = (fromCurrency == targetCurrency)
                    ? tx.Amount
                    : await _currencyService.ConvertAsync(tx.Amount, fromCurrency, targetCurrency);

                convertedTransactions.Add(new ConvertedTransaction
                {
                    Transaction = tx,
                    ConvertedAmount = convertedAmount
                });
            }

            // 3. Məbləğlərin Hesablanması (Tarix filtri varsa filtrlənmiş məlumatları, yoxdursa cari ayı əhatə edir)
            List<ConvertedTransaction> targetPeriodTxs;

            if (filterObj.StartDate.HasValue || filterObj.EndDate.HasValue)
            {
                targetPeriodTxs = convertedTransactions;
            }
            else
            {
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                targetPeriodTxs = convertedTransactions
                    .Where(ct => ct.Transaction.Date >= monthStart && ct.Transaction.Date < monthEnd)
                    .ToList();
            }

            decimal monthlyIncome = targetPeriodTxs.Where(ct => ct.Transaction.IsIncome).Sum(ct => ct.ConvertedAmount);
            decimal monthlyExpense = targetPeriodTxs.Where(ct => !ct.Transaction.IsIncome).Sum(ct => ct.ConvertedAmount);
            decimal monthlySavings = monthlyIncome - monthlyExpense;

            // 4. Əvvəlki Ayın Göstəriciləri
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var prevStart = currentMonthStart.AddMonths(-1);
            var prevEnd = currentMonthStart.AddTicks(-1);

            var prevTransactions = await _context.Transactions
                .Where(t => !t.IsDeleted && t.UserId == userId && t.Date >= prevStart && t.Date <= prevEnd)
                .ToListAsync();

            decimal prevIncome = 0;
            decimal prevExpense = 0;

            foreach (var tx in prevTransactions)
            {
                string fromCurrency = string.IsNullOrEmpty(tx.Currency) ? "AZN" : tx.Currency.ToUpper();
                decimal convertedAmount = (fromCurrency == targetCurrency)
                    ? tx.Amount
                    : await _currencyService.ConvertAsync(tx.Amount, fromCurrency, targetCurrency);

                if (tx.IsIncome) prevIncome += convertedAmount;
                else prevExpense += convertedAmount;
            }
            decimal prevSavings = prevIncome - prevExpense;

            // 5. Kateqoriyalara Görə Xərclər
            decimal filterTotalExpense = convertedTransactions.Where(ct => !ct.Transaction.IsIncome).Sum(ct => ct.ConvertedAmount);

            var topCategoryList = convertedTransactions
                .Where(ct => !ct.Transaction.IsIncome)
                .GroupBy(ct => ct.Transaction.Category != null && !string.IsNullOrWhiteSpace(ct.Transaction.Category.Name)
                    ? ct.Transaction.Category.Name
                    : "Kateqoriyasız")
                .Select(g => new TopCategoryVM
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(ct => ct.ConvertedAmount),
                    Percentage = filterTotalExpense > 0
                        ? Math.Round((double)(g.Sum(ct => ct.ConvertedAmount) / filterTotalExpense * 100), 1)
                        : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // 6. Son 6 Ayın Trend Məlumatları
            var trendPoints = new List<TrendPointVM>();
            var monthlyLabels = new List<string>();
            var monthlyIncomeData = new List<decimal>();
            var monthlyExpenseData = new List<decimal>();

            var sixMonthsAgo = currentMonthStart.AddMonths(-5);
            var trendTransactions = await _context.Transactions
                .Where(t => !t.IsDeleted && t.UserId == userId && t.Date >= sixMonthsAgo)
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var mStart = currentMonthStart.AddMonths(-i);
                var mEnd = mStart.AddMonths(1);

                var mTxs = trendTransactions.Where(t => t.Date >= mStart && t.Date < mEnd).ToList();

                decimal inc = 0;
                decimal exp = 0;

                foreach (var tx in mTxs)
                {
                    string fromCurr = string.IsNullOrEmpty(tx.Currency) ? "AZN" : tx.Currency.ToUpper();
                    decimal converted = (fromCurr == targetCurrency)
                        ? tx.Amount
                        : await _currencyService.ConvertAsync(tx.Amount, fromCurr, targetCurrency);

                    if (tx.IsIncome) inc += converted;
                    else exp += converted;
                }

                string label = mStart.ToString("MMM");
                trendPoints.Add(new TrendPointVM { Label = label, Income = inc, Expense = exp });
                monthlyLabels.Add(label);
                monthlyIncomeData.Add(inc);
                monthlyExpenseData.Add(exp);
            }

            // 7. Həftənin Günlərinə Görə Xərclər
            var daysOfWeek = new[] { "B.E", "Ç.Ə", "Ç", "C.A", "C", "Ş", "B" };
            var dayExpenses = new decimal[7];

            foreach (var ct in convertedTransactions.Where(ct => !ct.Transaction.IsIncome))
            {
                int dayIndex = ((int)ct.Transaction.Date.DayOfWeek + 6) % 7;
                dayExpenses[dayIndex] += ct.ConvertedAmount;
            }

            // 8. Yekun ReportVM
            return new ReportVM
            {
                Filter = filterObj,
                Kpi = new ReportKpiVM
                {
                    MonthlyIncome = monthlyIncome,
                    MonthlyExpense = monthlyExpense,
                    MonthlySavings = monthlySavings,
                    HealthScore = CalculateHealthScore(monthlyIncome, monthlyExpense),

                    IncomeChangePercent = CalculateChangePercent(prevIncome, monthlyIncome),
                    ExpenseChangePercent = CalculateChangePercent(prevExpense, monthlyExpense),
                    SavingsChangePercent = CalculateChangePercent(prevSavings, monthlySavings),

                    NeedsAmount = monthlyExpense * 0.50m,
                    WantsAmount = monthlyExpense * 0.30m,
                    SavingsAmount = monthlyExpense * 0.20m
                },
                Categories = new CategoryChartVM
                {
                    CategoryNames = topCategoryList.Select(c => c.CategoryName).ToList(),
                    CategoryExpenses = topCategoryList.Select(c => c.Amount).ToList(),
                    TopCategories = topCategoryList
                },
                Trend = new TrendChartVM
                {
                    Points = trendPoints,
                    MonthlyLabels = monthlyLabels,
                    MonthlyIncomeData = monthlyIncomeData,
                    MonthlyExpenseData = monthlyExpenseData
                },
                Weekdays = new WeekdayChartVM
                {
                    DayNames = daysOfWeek.ToList(),
                    DayExpenses = dayExpenses.ToList()
                }
            };
        }
            public async Task<ReportVM> GetGuestReportDataAsync(
    List<Transaction> guestTransactions,
    ReportFilterVM? filter = null)
        {
            var filterObj = filter ?? new ReportFilterVM();
            var now = DateTime.Now;

            string targetCurrency = string.IsNullOrWhiteSpace(filterObj.DisplayCurrency)
                ? "AZN"
                : filterObj.DisplayCurrency.ToUpper();

            // 1. Session-dakı əməliyyatları götürürük
            var transactions = guestTransactions
                .Where(t => !t.IsDeleted)
                .AsQueryable();

            if (filterObj.StartDate.HasValue)
            {
                var start = filterObj.StartDate.Value.Date;
                transactions = transactions.Where(t => t.Date >= start);
            }

            if (filterObj.EndDate.HasValue)
            {
                var end = filterObj.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                transactions = transactions.Where(t => t.Date <= end);
            }

            if (filterObj.CardId.HasValue && filterObj.CardId.Value > 0)
                transactions = transactions.Where(t => t.CardId == filterObj.CardId.Value);

            if (filterObj.CategoryId.HasValue && filterObj.CategoryId.Value > 0)
                transactions = transactions.Where(t => t.CategoryId == filterObj.CategoryId.Value);

            var transactionList = transactions.ToList();

            // 2. Məbləğləri seçilmiş valyutaya çeviririk
            var convertedTransactions = new List<ConvertedTransaction>();

            foreach (var tx in transactionList)
            {
                string fromCurrency = string.IsNullOrEmpty(tx.Currency)
                    ? "AZN"
                    : tx.Currency.ToUpper();

                decimal convertedAmount = fromCurrency == targetCurrency
                    ? tx.Amount
                    : await _currencyService.ConvertAsync(
                        tx.Amount,
                        fromCurrency,
                        targetCurrency);

                convertedTransactions.Add(new ConvertedTransaction
                {
                    Transaction = tx,
                    ConvertedAmount = convertedAmount
                });
            }
            // 3. Məbləğlərin Hesablanması
            List<ConvertedTransaction> targetPeriodTxs;

            if (filterObj.StartDate.HasValue || filterObj.EndDate.HasValue)
            {
                targetPeriodTxs = convertedTransactions;
            }
            else
            {
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                targetPeriodTxs = convertedTransactions
                    .Where(ct => ct.Transaction.Date >= monthStart &&
                                 ct.Transaction.Date < monthEnd)
                    .ToList();
            }

            decimal monthlyIncome = targetPeriodTxs
                .Where(ct => ct.Transaction.IsIncome)
                .Sum(ct => ct.ConvertedAmount);

            decimal monthlyExpense = targetPeriodTxs
                .Where(ct => !ct.Transaction.IsIncome)
                .Sum(ct => ct.ConvertedAmount);

            decimal monthlySavings = monthlyIncome - monthlyExpense;

            // 4. Əvvəlki ayın göstəriciləri
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var prevStart = currentMonthStart.AddMonths(-1);
            var prevEnd = currentMonthStart.AddTicks(-1);

            var prevTransactions = convertedTransactions
                .Where(ct => ct.Transaction.Date >= prevStart &&
                             ct.Transaction.Date <= prevEnd)
                .ToList();

            decimal prevIncome = prevTransactions
                .Where(ct => ct.Transaction.IsIncome)
                .Sum(ct => ct.ConvertedAmount);

            decimal prevExpense = prevTransactions
                .Where(ct => !ct.Transaction.IsIncome)
                .Sum(ct => ct.ConvertedAmount);

            decimal prevSavings = prevIncome - prevExpense;

            // 5. Kateqoriyalara görə xərclər
            decimal filterTotalExpense = convertedTransactions
                .Where(ct => !ct.Transaction.IsIncome)
                .Sum(ct => ct.ConvertedAmount);

            var topCategoryList = convertedTransactions
                .Where(ct => !ct.Transaction.IsIncome)
                .GroupBy(ct => ct.Transaction.Category != null &&
                               !string.IsNullOrWhiteSpace(ct.Transaction.Category.Name)
                    ? ct.Transaction.Category.Name
                    : "Kateqoriyasız")
                .Select(g => new TopCategoryVM
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(ct => ct.ConvertedAmount),
                    Percentage = filterTotalExpense > 0
                        ? Math.Round((double)(g.Sum(ct => ct.ConvertedAmount) /
                            filterTotalExpense * 100), 1)
                        : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();
            // 6. Son 6 ayın trend məlumatları
            var trendPoints = new List<TrendPointVM>();
            var monthlyLabels = new List<string>();
            var monthlyIncomeData = new List<decimal>();
            var monthlyExpenseData = new List<decimal>();

            var sixMonthsAgo = currentMonthStart.AddMonths(-5);

            for (int i = 5; i >= 0; i--)
            {
                var mStart = currentMonthStart.AddMonths(-i);
                var mEnd = mStart.AddMonths(1);

                var mTxs = convertedTransactions
                    .Where(ct => ct.Transaction.Date >= mStart &&
                                 ct.Transaction.Date < mEnd)
                    .ToList();

                decimal inc = mTxs
                    .Where(x => x.Transaction.IsIncome)
                    .Sum(x => x.ConvertedAmount);

                decimal exp = mTxs
                    .Where(x => !x.Transaction.IsIncome)
                    .Sum(x => x.ConvertedAmount);

                string label = mStart.ToString("MMM");

                trendPoints.Add(new TrendPointVM
                {
                    Label = label,
                    Income = inc,
                    Expense = exp
                });

                monthlyLabels.Add(label);
                monthlyIncomeData.Add(inc);
                monthlyExpenseData.Add(exp);
            }

            // 7. Həftənin günlərinə görə xərclər
            var daysOfWeek = new[] { "B.E", "Ç.Ə", "Ç", "C.A", "C", "Ş", "B" };
            var dayExpenses = new decimal[7];

            foreach (var ct in convertedTransactions.Where(x => !x.Transaction.IsIncome))
            {
                int dayIndex = ((int)ct.Transaction.Date.DayOfWeek + 6) % 7;
                dayExpenses[dayIndex] += ct.ConvertedAmount;
            }

            // 8. ReportVM qaytarırıq
            return new ReportVM
            {
                Filter = filterObj,

                Kpi = new ReportKpiVM
                {
                    MonthlyIncome = monthlyIncome,
                    MonthlyExpense = monthlyExpense,
                    MonthlySavings = monthlySavings,
                    HealthScore = CalculateHealthScore(monthlyIncome, monthlyExpense),

                    IncomeChangePercent = CalculateChangePercent(prevIncome, monthlyIncome),
                    ExpenseChangePercent = CalculateChangePercent(prevExpense, monthlyExpense),
                    SavingsChangePercent = CalculateChangePercent(prevSavings, monthlySavings),

                    NeedsAmount = monthlyExpense * 0.50m,
                    WantsAmount = monthlyExpense * 0.30m,
                    SavingsAmount = monthlyExpense * 0.20m
                },

                Categories = new CategoryChartVM
                {
                    CategoryNames = topCategoryList.Select(x => x.CategoryName).ToList(),
                    CategoryExpenses = topCategoryList.Select(x => x.Amount).ToList(),
                    TopCategories = topCategoryList
                },

                Trend = new TrendChartVM
                {
                    Points = trendPoints,
                    MonthlyLabels = monthlyLabels,
                    MonthlyIncomeData = monthlyIncomeData,
                    MonthlyExpenseData = monthlyExpenseData
                },

                Weekdays = new WeekdayChartVM
                {
                    DayNames = daysOfWeek.ToList(),
                    DayExpenses = dayExpenses.ToList()
                }
            };
        }


        private static decimal CalculateChangePercent(decimal previous, decimal current)
        {
            if (previous <= 0) return current > 0 ? 100 : 0;
            return Math.Round(((current - previous) / previous) * 100, 1);
        }

        private static int CalculateHealthScore(decimal income, decimal expense)
        {
            if (income <= 0) return 0;
            var ratio = (expense / income) * 100;
            if (ratio <= 50) return 90;
            if (ratio <= 80) return 70;
            if (ratio <= 100) return 50;
            return 20;
        }

        private class ConvertedTransaction
        {
            public Transaction Transaction { get; set; } = null!;
            public decimal ConvertedAmount { get; set; }
        }
    }
}