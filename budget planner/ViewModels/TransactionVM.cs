using System;

namespace budget_planner.ViewModels
{
    public class TransactionVM
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "AZN";
        public bool IsIncome { get; set; }
        public string? CategoryName { get; set; }
    }
}