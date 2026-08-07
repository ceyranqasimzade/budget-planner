using budget_planner.DAL;
using budget_planner.Extensions;
using budget_planner.Models;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
namespace budget_planner.Controllers
{
    public class GoalController : Controller
    {
        private readonly BudgetDbContext _context;
        private const string GuestGoalsKey = "Guest_Goals";
        // Defolt Dizayn Dəyərləri (Emerald Mövzusu ilə Sinxron)
        private const string DefaultCurrency = "AZN";
        private const string DefaultIcon = "bi-bullseye";
        private const string DefaultColor = "emerald";
        public GoalController(BudgetDbContext context)
        {
            _context = context;
        }
        #region --- 1. INDEX (LIST ALL GOALS) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<GoalVM> goals;

            if (IsUserAuthenticated())
            {
                var userId = GetCurrentUserId();
                goals = await _context.Goals
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .Select(x => new GoalVM
                    {
                        Id = x.Id,
                        Name = x.Name,
                        CurrentAmount = x.CurrentAmount,
                        TargetAmount = x.TargetAmount,
                        Currency = string.IsNullOrEmpty(x.Currency) ? DefaultCurrency : x.Currency,
                        IconClass = string.IsNullOrEmpty(x.IconClass) ? DefaultIcon : x.IconClass,
                        ColorClass = string.IsNullOrEmpty(x.ColorClass) ? DefaultColor : x.ColorClass,
                        Deadline = x.Deadline
                    })
                    .ToListAsync();
            }
            else
            {
                goals = GetGuestGoals();
            }
            var model = new GoalsVM { Goals = goals };
            return View(model);
        }
        #endregion
        #region --- 2. CREATE (GET & POST) ---
        [HttpGet]
        public IActionResult Create()
        {
            var defaultGoal = new GoalVM
            {
                Currency = DefaultCurrency,
                IconClass = DefaultIcon,
                ColorClass = DefaultColor
            };
            return View(defaultGoal);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GoalVM model)
        {
            if (!ModelState.IsValid) return View(model);

            // Defolt dəyər təminatı
            model.Currency = string.IsNullOrEmpty(model.Currency) ? DefaultCurrency : model.Currency;
            model.IconClass = string.IsNullOrEmpty(model.IconClass) ? DefaultIcon : model.IconClass;
            model.ColorClass = string.IsNullOrEmpty(model.ColorClass) ? DefaultColor : model.ColorClass;
            if (IsUserAuthenticated())
            {
                var userId = GetCurrentUserId();
                var goal = new Goal
                {
                    Name = model.Name,
                    TargetAmount = model.TargetAmount,
                    CurrentAmount = model.CurrentAmount,
                    Currency = model.Currency,
                    IconClass = model.IconClass,
                    ColorClass = model.ColorClass,
                    Deadline = model.Deadline,
                    UserId = userId
                };
                _context.Goals.Add(goal);
                await _context.SaveChangesAsync();
            }
            else
            {
                var guestGoals = GetGuestGoals();
                model.Id = guestGoals.Any() ? guestGoals.Max(g => g.Id) + 1 : 1;
                guestGoals.Add(model);
                SaveGuestGoals(guestGoals);
            }
            TempData["SuccessMessage"] = "Yeni maliyyə hədəfiniz uğurla yaradıldı!";
            return RedirectToAction(nameof(Index));
        }
        #endregion
        #region --- 3. EDIT (GET & POST) ---
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            GoalVM model = null;
            if (IsUserAuthenticated())
            {
                var userId = GetCurrentUserId();
                var goal = await _context.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
                if (goal != null)
                {
                    model = MapToGoalVM(goal);
                }
            }
            else
            {
                var guestGoals = GetGuestGoals();
                model = guestGoals.FirstOrDefault(g => g.Id == id);
            }
            if (model == null) return NotFound();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GoalVM model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            if (IsUserAuthenticated())
            {
                var userId = GetCurrentUserId();
                var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
                if (goal == null) return NotFound();
                goal.Name = model.Name;
                goal.TargetAmount = model.TargetAmount;
                goal.CurrentAmount = model.CurrentAmount;
                goal.Currency = model.Currency;
                goal.IconClass = model.IconClass;
                goal.ColorClass = model.ColorClass;
                goal.Deadline = model.Deadline;
                _context.Goals.Update(goal);
                await _context.SaveChangesAsync();
            }
            else
            {
                var guestGoals = GetGuestGoals();
                var index = guestGoals.FindIndex(g => g.Id == id);
                if (index == -1) return NotFound();

                guestGoals[index] = model;
                SaveGuestGoals(guestGoals);
            }
            TempData["SuccessMessage"] = "Hədəf uğurla yeniləndi!";
            return RedirectToAction(nameof(Index));
        }
        #endregion
        #region --- 4. DETAILS ---
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            GoalVM model = null;
            if (IsUserAuthenticated())
            {
                var userId = GetCurrentUserId();
                var goal = await _context.Goals.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

                if (goal != null)
                {
                    model = MapToGoalVM(goal);
                }
            }
            else
            {
                var guestGoals = GetGuestGoals();
                model = guestGoals.FirstOrDefault(g => g.Id == id);
            }
            if (model == null) return NotFound();
            return View(model);
        }
        #endregion
        #region --- 5. DELETE ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (IsUserAuthenticated())
            {
                var userId = GetCurrentUserId();
                var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
                if (goal != null)
                {
                    _context.Goals.Remove(goal);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Hədəf uğurla silindi.";
                }
            }
            else
            {
                var guestGoals = GetGuestGoals();
                var goalToRemove = guestGoals.FirstOrDefault(g => g.Id == id);
                if (goalToRemove != null)
                {
                    guestGoals.Remove(goalToRemove);
                    SaveGuestGoals(guestGoals);
                    TempData["SuccessMessage"] = "Hədəf uğurla silindi.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion
        #region --- PRIVATE HELPER METHODS (KODU TƏMİZ VƏ DRY SAXLAMAQ ÜÇÜN) ---
        private bool IsUserAuthenticated()
        {
            return User.Identity != null && User.Identity.IsAuthenticated;
        }
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        private List<GoalVM> GetGuestGoals()
        {
            return HttpContext.Session.GetObject<List<GoalVM>>(GuestGoalsKey) ?? new List<GoalVM>();
        }
        private void SaveGuestGoals(List<GoalVM> goals)
        {
            HttpContext.Session.SetObject(GuestGoalsKey, goals);
        }
        private static GoalVM MapToGoalVM(Goal goal)
        {
            return new GoalVM
            {
                Id = goal.Id,
                Name = goal.Name,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = goal.CurrentAmount,
                Currency = string.IsNullOrEmpty(goal.Currency) ? DefaultCurrency : goal.Currency,
                IconClass = string.IsNullOrEmpty(goal.IconClass) ? DefaultIcon : goal.IconClass,
                ColorClass = string.IsNullOrEmpty(goal.ColorClass) ? DefaultColor : goal.ColorClass,
                Deadline = goal.Deadline
            };
        }
        #endregion
    }
}