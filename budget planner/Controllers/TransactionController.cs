using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace budget_planner.Controllers
{
     [Authorize]
    public class TransactionController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionController(BudgetDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create(TransactionCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            var card = await _context.Cards.FindAsync(model.CardId);

            if (user != null && card != null && !card.IsDeleted)
            {
                if (model.IsIncome)
                {
                    card.Balance += model.Amount;
                }
                else
                {
                    if (card.Balance < model.Amount)
                    {
                        TempData["ErrorMessage"] = "Kartda kifayət qədər vəsait yoxdur!";
                        return RedirectToAction("Index", "Home");
                    }
                    card.Balance -= model.Amount;
                }

                var defaultCategory = _context.Categories.FirstOrDefault();
                if (defaultCategory == null)
                {
                    defaultCategory = new Category { Name = "Ümumi", Type = "Müxtəlif" };
                    _context.Categories.Add(defaultCategory);
                    await _context.SaveChangesAsync();
                }

                var transaction = new Transaction
                {
                    CardId = model.CardId,
                    Amount = model.Amount,
                    Description = model.Description,
                    IsIncome = model.IsIncome,
                    Date = DateTime.Now,
                    Currency = card.Currency,
                    UserId = user.Id,
                    CategoryId = defaultCategory.Id,
                    Status = "Tamamlandı"
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Əməliyyat uğurla qeydə alındı!";
            }

            return RedirectToAction("Index", "Home");
        }
    }
}