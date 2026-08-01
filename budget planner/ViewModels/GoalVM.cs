namespace budget_planner.ViewModels
{
    public class GoalVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fas fa-bullseye";

        // Eksik olan ColorClass əlavə edildi
        public string ColorClass { get; set; } = "primary";

        public decimal CurrentAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public string CurrencySymbol { get; set; } = "₼";
        public DateTime TargetDate { get; set; }

        public int Percentage => TargetAmount <= 0
            ? 0
            : (int)Math.Round((CurrentAmount / TargetAmount) * 100);

        public int MonthsRemaining => Math.Max(0, ((TargetDate.Year - DateTime.Now.Year) * 12) + TargetDate.Month - DateTime.Now.Month);
    }
}