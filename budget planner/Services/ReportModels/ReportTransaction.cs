using System;

namespace budget_planner.Services.ReportModels
{
    internal class ReportTransaction
    {
        public DateTime Date { get; init; }
        public decimal Amount { get; init; } // DisplayCurrency məbləğində
        public bool IsIncome { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public int? CardId { get; init; }
    }
}