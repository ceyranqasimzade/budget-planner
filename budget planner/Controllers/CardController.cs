using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace budget_planner.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CardController(BudgetDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CardCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var newCard = new Card
                {
                    CardName = model.CardName,
                    Last4Digits = model.Last4Digits,
                    Currency = model.Currency,
                    Balance = model.Balance,
                    UserId = user.Id
                };
                _context.Cards.Add(newCard);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kart uğurla əlavə edildi!";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(TransferVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            if (model.FromCardId == model.ToCardId)
            {
                TempData["ErrorMessage"] = "Göndərən və alan kart eyni ola bilməz!";
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            var fromCard = await _context.Cards.FindAsync(model.FromCardId);
            var toCard = await _context.Cards.FindAsync(model.ToCardId);

            if (fromCard != null && toCard != null && !fromCard.IsDeleted && !toCard.IsDeleted)
            {
                if (fromCard.Balance < model.Amount)
                {
                    TempData["ErrorMessage"] = "Göndərən kartda kifayət qədər vəsait yoxdur!";
                    return RedirectToAction("Index", "Home");
                }

                fromCard.Balance -= model.Amount;
                toCard.Balance += model.Amount;

                var category = _context.Categories.FirstOrDefault(c => c.Name == "Transfer") ?? _context.Categories.FirstOrDefault();

                _context.Transactions.Add(new Transaction
                {
                    CardId = model.FromCardId,
                    Amount = model.Amount,
                    Description = $"Transfer -> {toCard.CardName}",
                    IsIncome = false,
                    Date = DateTime.Now,
                    Currency = fromCard.Currency,
                    UserId = user!.Id,
                    CategoryId = category!.Id
                });

                _context.Transactions.Add(new Transaction
                {
                    CardId = model.ToCardId,
                    Amount = model.Amount,
                    Description = $"Transfer <- {fromCard.CardName}",
                    IsIncome = true,
                    Date = DateTime.Now,
                    Currency = toCard.Currency,
                    UserId = user!.Id,
                    CategoryId = category!.Id
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Köçürmə uğurla həyata keçirildi!";
            }

            return RedirectToAction("Index", "Home");
        }
    }
}