using System;

namespace budget_planner.ViewModels
{
    public class SubscriptionVM
    {
        public string Name { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime NextPaymentDate { get; set; }
        public string IconClass { get; set; } = null!;
        public string ColorClass { get; set; } = null!;
    }
}