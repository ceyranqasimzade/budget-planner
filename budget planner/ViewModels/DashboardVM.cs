using budget_planner.Models;
using System.Collections.Generic;

namespace budget_planner.ViewModels
{
    public class DashboardVM
    {
        public decimal TotalBalance { get; set; }
        public decimal CashBalance { get; set; } // Nağd pul balansı
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }

        // --- FAİZ (TREND) XASSƏLƏRİ ---
        public decimal? IncomeTrend { get; set; }
        public decimal? ExpenseTrend { get; set; }

        // --- XƏBƏRDARLIQ VƏ MƏSLƏHƏT XASSƏLƏRİ ---
        public string? BudgetWarning { get; set; }
        public string? FinancialAdvice { get; set; }

        // --- BİLDİRİŞLƏR SİYAHISI ---
        public List<NotificationVM> Notifications { get; set; } = new List<NotificationVM>();

        public int ActiveBudgetId { get; set; } = 1;

        public string BaseCurrencySymbol { get; set; } = "₼";
        public List<CurrencyRateVM> ExchangeRates { get; set; } = new List<CurrencyRateVM>();

        public List<CardVM> Cards { get; set; } = new List<CardVM>();
        public List<TransactionVM> LastTransactions { get; set; } = new List<TransactionVM>();

        // Modallarda göstərmək üçün Kateqoriyalar
        public List<CategorySelectVM> Categories { get; set; } = new List<CategorySelectVM>();

        // Qrafik (Chart.js) üçün kateqoriya üzrə xərclərin bölgüsü
        public List<CategoryExpenseVM> CategoryExpenses { get; set; } = new List<CategoryExpenseVM>();

        // Abunəliklər və Hədəflər siyahıları
        public List<SubscriptionVM> UpcomingPayments { get; set; } = new List<SubscriptionVM>();
        public List<GoalVM> ActiveGoals { get; set; } = new List<GoalVM>();
    }
}