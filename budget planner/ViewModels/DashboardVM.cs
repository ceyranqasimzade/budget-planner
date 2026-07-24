using System.Collections.Generic;

namespace budget_planner.ViewModels
{
    public class DashboardVM
    {
        public decimal TotalBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public int ActiveBudgetId { get; set; } = 1;

        public string BaseCurrencySymbol { get; set; } = "₼";
        public List<CurrencyRateVM> ExchangeRates { get; set; } = new List<CurrencyRateVM>();

        public List<CardVM> Cards { get; set; } = new List<CardVM>();
        public List<TransactionVM> LastTransactions { get; set; } = new List<TransactionVM>();
    }
}