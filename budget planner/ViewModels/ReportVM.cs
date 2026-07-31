using System.Collections.Generic;

namespace budget_planner.ViewModels
{
    public class ReportVM
    {
        // KPI Kartları & Sağlamlıq Skoru
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpense { get; set; }
        public decimal MonthlySavings { get; set; }
        public int HealthScore { get; set; } // 0 - 100 arası

        // Müqayisələr (Bu ay vs Keçən ay faizlə)
        public decimal IncomeChangePercent { get; set; }
        public decimal ExpenseChangePercent { get; set; }
        public decimal SavingsChangePercent { get; set; }

        // Qrafik 1: Gəlir vs Xərc Trendi (Son 6 ay)
        public List<string> MonthlyLabels { get; set; } = new(); // ["Yan", "Fev", "Mar", ...]
        public List<decimal> MonthlyIncomeData { get; set; } = new();
        public List<decimal> MonthlyExpenseData { get; set; } = new();

        // Qrafik 2: Kateqoriyalara görə xərc bölgüsü
        public List<string> CategoryNames { get; set; } = new();
        public List<decimal> CategoryExpenses { get; set; } = new();

        // Qrafik 3: Həftənin günlərinə görə xərc
        public List<string> DayNames { get; set; } = new(); // ["Bazar ertəsi", "Çərşənbə axşamı", ...]
        public List<decimal> DayExpenses { get; set; } = new();

        // Top Xərc Kateqoriyaları
        public List<TopCategoryVM> TopCategories { get; set; } = new();
    }

    public class TopCategoryVM
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }
}