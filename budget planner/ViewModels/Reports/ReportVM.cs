using System.Collections.Generic;

namespace budget_planner.ViewModels.Reports
{
    public class ReportVM
    {
        public ReportFilterVM Filter { get; set; } = new();
        public ReportKpiVM Kpi { get; set; } = new();
        public TrendChartVM Trend { get; set; } = new();
        public CategoryChartVM Categories { get; set; } = new();
        public WeekdayChartVM Weekdays { get; set; } = new();
    }

    public class ReportKpiVM
    {
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpense { get; set; }
        public decimal MonthlySavings { get; set; }
        public int HealthScore { get; set; }

        public decimal IncomeChangePercent { get; set; }
        public decimal ExpenseChangePercent { get; set; }
        public decimal SavingsChangePercent { get; set; }

        // 50/30/20 Qaydası
        public decimal NeedsAmount { get; set; }
        public decimal WantsAmount { get; set; }
        public decimal SavingsAmount { get; set; }
    }

    public class TrendChartVM
    {
        public List<TrendPointVM> Points { get; set; } = new();
        public List<string> MonthlyLabels { get; set; } = new();
        public List<decimal> MonthlyIncomeData { get; set; } = new();
        public List<decimal> MonthlyExpenseData { get; set; } = new();
    }

    public class TrendPointVM
    {
        public string Label { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal? Forecast { get; set; }
    }

    public class CategoryChartVM
    {
        public List<string> CategoryNames { get; set; } = new();
        public List<decimal> CategoryExpenses { get; set; } = new();
        public List<TopCategoryVM> TopCategories { get; set; } = new();
    }

    public class TopCategoryVM
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }

    public class WeekdayChartVM
    {
        public List<string> DayNames { get; set; } = new();
        public List<decimal> DayExpenses { get; set; } = new();
    }
}