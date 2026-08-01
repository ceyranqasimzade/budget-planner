using System.Collections.Generic;

namespace budget_planner.ViewModels
{
    public class GoalsVM
    {
        // Sizin artıq var olan GoalVM klassınızın siyahısı
        public List<GoalVM> Goals { get; set; } = new List<GoalVM>();

        // Motivasiya sözləri
        public string Quote { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }
}