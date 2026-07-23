using budget_planner.DAL;
using budget_planner.ViewModels; // ViewModellərimiz buradadır
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
// using budget_planner.Models; // Əgər DbContext və cədvəlləriniz Models qovluğundadırsa, bu sətrin qarşısındakı // işarəsini silin
// using budget_planner.Data; // Əgər DbContext Data qovluğundadırsa, bu sətrin qarşısındakı // işarəsini silin

namespace budget_planner.Controllers
{
    public class HomeController : Controller
    {
        // DİQQƏT: ApplicationDbContext yerinə öz layihənizdəki DbContext adını yazın 
        // (məsələn: AppDbContext, BudgetDbContext və s.)
        private readonly BudgetDbContext _context;

        public HomeController(BudgetDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Yeni ViewModel obyektini yaradırıq
            var vm = new DashboardVM();

            // 1. Bazadan kartları çəkirik
            var cards = _context.Cards.ToList();

            // Kartları CardVM formatına salırıq
            vm.Cards = cards.Select(c => new CardVM
            {
                Id = c.Id,
                CardName = c.CardName, // Əgər bazada cədvəlinizdə fərqlidirsə (məs: Name), onu yazın
                Last4Digits = c.Last4Digits,
                Currency = c.Currency ?? "AZN",
                Balance = c.Balance
            }).ToList();

            // 2. Ümumi balansı hesablayırıq (Bütün kartların balanslarının cəmi)
            vm.TotalBalance = vm.Cards.Sum(c => c.Balance);

            // 3. Bu ayın gəlir və xərclərini hesablayırıq
            var currentMonth = System.DateTime.Now.Month;
            var currentYear = System.DateTime.Now.Year;

            // Bazadan yalnız bu aya aid tranzaksiyaları çəkirik
            var thisMonthTransactions = _context.Transactions
                .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .ToList();

            // Gəlirləri və Xərcləri toplayırıq
            vm.TotalIncome = thisMonthTransactions.Where(t => t.IsIncome == true).Sum(t => t.Amount);
            vm.TotalExpense = thisMonthTransactions.Where(t => t.IsIncome == false).Sum(t => t.Amount);

            // Sonda hazırladığımız bu məlumatları View-a göndəririk
            return View(vm);
        }

        // Xəta səhifəsi üçün standart metod
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}