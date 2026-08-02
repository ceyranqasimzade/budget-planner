using System;
using System.Collections.Generic;
using System.Linq;

namespace budget_planner.ViewModels
{
    public class GoalVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal CurrentAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public string Currency { get; set; } = "AZN";
        public string IconClass { get; set; } = "bi-house-heart-fill";
        public string ColorClass { get; set; } = "primary";
        public DateTime? Deadline { get; set; }
        public string CategoryName { get; set; } = "Ümumi";

        // Hesablanan property-lər
        public bool IsCompleted => CurrentAmount >= TargetAmount && TargetAmount > 0;

        public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);

        public int Percentage => TargetAmount <= 0 ? 0 :
            (int)Math.Min((CurrentAmount / TargetAmount) * 100, 100);

        public int MonthsRemaining
        {
            get
            {
                if (!Deadline.HasValue || Deadline.Value <= DateTime.Now)
                    return 0;

                return ((Deadline.Value.Year - DateTime.Now.Year) * 12)
                     + (Deadline.Value.Month - DateTime.Now.Month);
            }
        }

        public string CurrencySymbol
        {
            get
            {
                return Currency switch
                {
                    "USD" => "$",
                    "EUR" => "€",
                    "TRY" => "₺",
                    "GBP" => "£",
                    _ => "₼"
                };
            }
        }
    }

    public class GoalsVM
    {
        public List<GoalVM> Goals { get; set; } = new();

        public int ActiveGoalsCount => Goals.Count(g => !g.IsCompleted);
        public int CompletedGoalsCount => Goals.Count(g => g.IsCompleted);
        public decimal TotalSavedAmount => Goals.Sum(g => g.CurrentAmount);
        public string DefaultCurrencySymbol => Goals.FirstOrDefault()?.CurrencySymbol ?? "₼";
    }
}