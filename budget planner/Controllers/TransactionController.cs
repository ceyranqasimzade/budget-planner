using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
namespace budget_planner.Controllers
{
    public class TransactionController : Controller
    {
        private BudgetDbContext _context { get; }

        public TransactionController(BudgetDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var transactions = await _context.Transactions.Where(t => !t.IsDeleted).OrderByDescending(t => t.Date).ToListAsync();
            decimal totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            return View(transactions);
        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVM createVM)
        {
            if (!ModelState.IsValid)
            {
                return View(createVM);
            }

            Transaction newTransaction = new Transaction
            {
                Description = createVM.Description,
                Amount = createVM.Amount,
                Category = createVM.Category,
                Date = createVM.Date,
                Status = createVM.Status,
                IsIncome = createVM.IsIncome,
                Currency = createVM.Currency, 
                IsDeleted = false
            };
            await _context.AddAsync(newTransaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Update(int? Id)
        {
            if (Id == null)
            {
                return BadRequest();
            }

            Transaction existTransaction = await _context.Transactions.Where(t => !t.IsDeleted).FirstOrDefaultAsync(t => t.Id == Id);
            if (existTransaction == null)
            {
                return NotFound();
            }
            return View(existTransaction);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? Id, Transaction transaction)
        {
            if (Id == null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return View(transaction);
            }
            Transaction existTransaction = await _context.Transactions.Where(t => !t.IsDeleted).FirstOrDefaultAsync(t => t.Id == Id);
            if (existTransaction == null)
            {
                return NotFound();
            }
            existTransaction.Description = transaction.Description;
            existTransaction.Amount = transaction.Amount;
            existTransaction.Category = transaction.Category;
            existTransaction.Date = transaction.Date;
            existTransaction.Status = transaction.Status;
            existTransaction.IsIncome = transaction.IsIncome;
            existTransaction.Currency = transaction.Currency;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int? Id)
        {
            if (Id == null)
            {
                return BadRequest();
            }
            Transaction existTransaction = await _context.Transactions.Where(t => !t.IsDeleted).FirstOrDefaultAsync(t => t.Id == Id);
            if (existTransaction == null)
            {
                return NotFound();
            }
            existTransaction.IsDeleted = true; 
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}