using budget_planner.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace budget_planner.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}