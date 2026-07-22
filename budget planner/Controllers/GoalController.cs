using Microsoft.AspNetCore.Mvc;

namespace budget_planner.Controllers
{
    public class GoalController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}