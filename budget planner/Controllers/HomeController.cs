using budget_planner.DAL;
using budget_planner.ViewModels;
using budget_planner.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace budget_planner.Controllers
{
    public class HomeController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly CurrencyService _currencyService;

        public HomeController(BudgetDbContext context, CurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardVM();

            // 1. Bazadan kartları çəkirik
            var cards = _context.Cards.ToList();

            vm.Cards = cards.Select(c => new CardVM
            {
                Id = c.Id,
                CardName = c.CardName,
                Last4Digits = c.Last4Digits,
                Currency = c.Currency ?? "AZN",
                Balance = c.Balance
            }).ToList();

            // 2. Ümumi balansı hesablayırıq
            vm.TotalBalance = vm.Cards.Sum(c => c.Balance);

            // ========================================================
            // YENİLƏNƏN HİSSƏ BURADIR:
            // ========================================================
            // 3. Bu ayın gəlir və xərclərini hesablayırıq
            var currentMonth = System.DateTime.Now.Month;
            var currentYear = System.DateTime.Now.Year;

            // .ToList() sildik ki, sorğu hələ icra olunmasın
            var thisMonthTransactions = _context.Transactions
                .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear);

            vm.TotalIncome = thisMonthTransactions.Where(t => t.IsIncome == true).Sum(t => t.Amount);
            vm.TotalExpense = thisMonthTransactions.Where(t => t.IsIncome == false).Sum(t => t.Amount);
            // ========================================================

            // 4. VALYUTA VƏ SİMVOL PARAMETRLƏRİ
            vm.BaseCurrencySymbol = "₼";

            // API-dən məzənnələri çəkirik və Model-ə mənimsədirik
            vm.ExchangeRates = await _currencyService.GetExchangeRatesAsync();

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}