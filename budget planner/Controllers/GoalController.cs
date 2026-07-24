using budget_planner.DAL;
using budget_planner.ViewModels; // ViewModels qovluğunu əlavə edirik
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace budget_planner.Controllers
{
    public class GoalController : Controller
    {
        private readonly BudgetDbContext _context;

        public GoalController(BudgetDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Bazadan verilənləri çəkib GoalVM-ə çeviririk (Mapping)
            var goalsVM = await _context.Goals
                .Select(g => new GoalVM
                {
                    Name = g.Title, // 👈 Goal modelindəki Title xassəsi GoalVM-dəki Name-ə mənimsədildi
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    IconClass = g.IconClass ?? "bi-flag-fill",
                    ColorClass = g.ColorClass ?? "bg-primary"
                })
                .ToListAsync();

            return View(goalsVM);
        }
    }
}