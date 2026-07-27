using budget_planner.DAL;
using budget_planner.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace budget_planner.Controllers
{
    public class UpcomingPaymentController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UpcomingPaymentController(BudgetDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Bütün ödənişlərin siyahısı səhifəsi
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // 🟢 1-Cİ HƏLL: Qonaqlar üçün Login əvəzinə boş siyahı və boş kartlar göndəririk
            if (user == null)
            {
                ViewBag.Cards = new List<Card>();
                return View(new List<UpcomingPayment>());
            }

            var payments = await _context.UpcomingPayments
                .Where(u => u.UserId == user.Id)
                .OrderBy(u => u.IsPaid)
                .ThenBy(u => u.DueDate)
                .ToListAsync();

            // KARTLARI (Hesabları) TAPIB SƏHİFƏYƏ GÖNDƏRİRİK
            ViewBag.Cards = await _context.Set<Card>()
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            return View(payments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UpcomingPayment model)
        {
            var user = await _userManager.GetUserAsync(User);

            // 🟢 2-Cİ HƏLL: Qonaq istifadəçi üçün sınaq rejimi bildirişi
            if (user == null)
            {
                TempData["SuccessMessage"] = "Qarşıdan gələn ödəniş sınaq rejimində əlavə edildi! Məlumatlar yadda saxlanılmayacaq.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                model.UserId = user.Id;
                model.IsPaid = false;

                _context.UpcomingPayments.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Qarşıdan gələn ödəniş uğurla əlavə edildi!";
            }
            else
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Xəta sahəsi: {error.Key}");
                    foreach (var item in error.Value.Errors)
                    {
                        Console.WriteLine($"Səbəb: {item.ErrorMessage}");
                    }
                }
                TempData["ErrorMessage"] = "Məlumatları düzgün daxil etdiyinizdən əmin olun.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Ödənildi olaraq işarələ və avtomatik Xərc yaradaraq balansı azalt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id, string paymentMethod)
        {
            var user = await _userManager.GetUserAsync(User);

            // 🟢 3-CÜ HƏLL: Sınaq rejimi bildirişi
            if (user == null)
            {
                TempData["SuccessMessage"] = "Ödəniş sınaq rejimində icra olundu! Saytdan çıxdıqda sıfırlanacaq.";
                return RedirectToAction(nameof(Index));
            }

            var payment = await _context.UpcomingPayments
                .FirstOrDefaultAsync(u => u.Id == id && u.UserId == user.Id);

            if (payment != null && !payment.IsPaid)
            {
                Card? selectedCard = null;

                // --- BALANS YOXLANIŞI (VALIDATION) ---

                // Əgər KART seçilibsə
                if (paymentMethod != "cash" && int.TryParse(paymentMethod, out int selectedCardId))
                {
                    selectedCard = await _context.Set<Card>()
                        .FirstOrDefaultAsync(c => c.Id == selectedCardId && c.UserId == user.Id);

                    if (selectedCard == null)
                    {
                        TempData["ErrorMessage"] = "Seçilmiş kart tapılmadı!";
                        return RedirectToAction(nameof(Index));
                    }

                    // Kartda kifayət qədər pul yoxdursa əməliyyatı dayandırırıq
                    if (selectedCard.Balance < payment.Amount)
                    {
                        TempData["ErrorMessage"] = $"Seçilmiş kartda kifayət qədər vəsait yoxdur! (Balans: {selectedCard.Balance} {payment.Currency})";
                        return RedirectToAction(nameof(Index));
                    }
                }
                // Əgər NAĞD pul seçilibsə
                else if (paymentMethod == "cash")
                {
                    // Nağd balansda kifayət qədər pul yoxdursa əməliyyatı dayandırırıq
                    if (user.CashBalance < payment.Amount)
                    {
                        TempData["ErrorMessage"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Mövcud Nağd: {user.CashBalance} {payment.Currency})";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // --- BALANS KİFAYƏTDİRSƏ ÖDƏNİŞ İCRA OLUNUR ---

                var defaultCategory = await _context.Categories.FirstOrDefaultAsync();
                if (defaultCategory == null)
                {
                    defaultCategory = new Category
                    {
                        Name = "Xərclər",
                        Type = "Expense",
                        Icon = "default.png"
                    };
                    _context.Categories.Add(defaultCategory);
                    await _context.SaveChangesAsync();
                }

                payment.IsPaid = true;

                var newExpense = new Transaction
                {
                    UserId = user.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Date = DateTime.Now,
                    Description = $"{payment.Title} (Avtomatik)",
                    IsIncome = false,
                    Status = "Tamamlandı",
                    CategoryId = defaultCategory.Id
                };

                // Çıxılma əməliyyatları
                if (selectedCard != null)
                {
                    newExpense.CardId = selectedCard.Id;
                    selectedCard.Balance -= payment.Amount;
                    _context.Update(selectedCard);
                    user.TotalBalance -= payment.Amount;
                }
                else if (paymentMethod == "cash")
                {
                    user.CashBalance -= payment.Amount;
                    user.TotalBalance -= payment.Amount;
                }

                await _userManager.UpdateAsync(user);
                _context.Transactions.Add(newExpense);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ödəniş uğurla həyata keçirildi!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            // 🟢 4-CÜ HƏLL: Sınaq rejimi bildirişi
            if (user == null)
            {
                TempData["SuccessMessage"] = "Ödəniş sınaq rejimində silindi!";
                return RedirectToAction(nameof(Index));
            }

            var payment = await _context.UpcomingPayments
                .FirstOrDefaultAsync(u => u.Id == id && u.UserId == user.Id);

            if (payment != null)
            {
                _context.UpcomingPayments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ödəniş silindi.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}