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
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string currency = "AZN"; // Nağd əməliyyat üçün susmaya görə valyuta

            // 1. KART BALANSININ YENİLƏNMƏSİ (Əgər kart seçilibsə)
            if (model.CardId.HasValue && model.CardId.Value > 0)
            {
                var card = await _context.Cards.FindAsync(model.CardId);

                if (card == null || card.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Seçilmiş kart tapılmadı və ya silinib!";
                    return RedirectToAction("Index", "Home");
                }

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

                currency = card.Currency; // Kartın öz valyutası
            }

            // 2. KATEQORİYA MƏNTİQİ (Siyahıdan seçilibsə / yeni yazılıbsa / boşdursa)
            int categoryId;
            if (!string.IsNullOrWhiteSpace(model.CategoryName))
            {
                var categoryNameClean = model.CategoryName.Trim();
                var category = _context.Categories
                    .FirstOrDefault(c => c.Name.ToLower() == categoryNameClean.ToLower());

                // Əgər yazılan kateqoriya bazada yoxdursa, yeni yaradılır
                if (category == null)
                {
                    category = new Category
                    {
                        Name = categoryNameClean,
                        Type = model.IsIncome ? "Gəlir" : "Xərc"
                    };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                categoryId = category.Id;
            }
            else
            {
                // Kateqoriya boş buraxılıbsa "Ümumi" kateqoriyası istifadə olunur
                var defaultCategory = _context.Categories.FirstOrDefault(c => c.Name == "Ümumi");
                if (defaultCategory == null)
                {
                    defaultCategory = new Category { Name = "Ümumi", Type = "Müxtəlif" };
                    _context.Categories.Add(defaultCategory);
                    await _context.SaveChangesAsync();
                }

                categoryId = defaultCategory.Id;
            }

            // 3. ƏMƏLİYYATIN BAZAYA YAZILMASI
            var transaction = new Transaction
            {
                CardId = model.CardId, // Nağd olduqda null olaraq yazılır
                Amount = model.Amount,
                Description = model.Description,
                IsIncome = model.IsIncome,
                Date = DateTime.Now,
                Currency = currency,
                UserId = user.Id,
                CategoryId = categoryId,
                Status = "Tamamlandı"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Əməliyyat uğurla qeydə alındı!";
            return RedirectToAction("Index", "Home");
        }
    }
}