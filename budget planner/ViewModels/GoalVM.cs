using System;

namespace budget_planner.ViewModels
{
    public class GoalVM
    {
        public int Id { get; set; }   // <-- Bunu əlavə et

        public string Name { get; set; } = null!;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime Deadline { get; set; } // 👈 Goal modelinizdəki Deadline üçün əlavə olundu
        public string IconClass { get; set; } = null!;
        public string ColorClass { get; set; } = null!;

        // Faiz hesablayan köməkçi xassə
        public double Percentage => TargetAmount > 0
            ? (double)(CurrentAmount / TargetAmount) * 100
            : 0;
    }
}