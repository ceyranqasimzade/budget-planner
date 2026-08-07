using budget_planner.DAL;
using budget_planner.Extensions;
using budget_planner.Models;
using budget_planner.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<UpcomingPaymentController> _logger;
        public UpcomingPaymentController(
            BudgetDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrencyService currencyService,
            ILogger<UpcomingPaymentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _currencyService = currencyService;
            _logger = logger;
        }
        // Köməkçi Metod: Valyuta kodunu standartlaşdırır (DRY Prinsipi)
        private static string NormalizeCurrency(string? currency)
        {
            return string.IsNullOrWhiteSpace(currency) ? "AZN" : currency.Trim().ToUpper();
        }
        // Köməkçi Metod: Sorğunun gəldiyi səhifəyə (Referer) və ya standart olaraq Index-ə yönləndirir
        private IActionResult RedirectToReferrerOrIndex()
        {
            string? referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                var localPath = uri.PathAndQuery;
                if (Url.IsLocalUrl(localPath))
                {
                    return Redirect(localPath);
                }
            }
            return RedirectToAction(nameof(Index));
        }
        // ==========================================
        // GET: /UpcomingPayment/Index
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestPayments = HttpContext.Session.GetObject<List<UpcomingPayment>>("Guest_UpcomingPayments")
                                     ?? new List<UpcomingPayment>();
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards")
                                  ?? new List<Card>();
                ViewBag.Cards = guestCards;
                return View(guestPayments.OrderBy(u => u.IsPaid).ThenBy(u => u.DueDate).ToList());
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var payments = await _context.UpcomingPayments
                .Where(u => u.UserId == user.Id)
                .OrderBy(u => u.IsPaid)
                .ThenBy(u => u.DueDate)
                .ToListAsync();
            ViewBag.Cards = await _context.Cards
                .Where(c => c.UserId == user.Id && !c.IsDeleted)
                .ToListAsync();
            return View(payments);
        }
        // ==========================================
        // POST: /UpcomingPayment/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UpcomingPayment model)
        {
            if (model.Amount <= 0)
            {
                TempData["Error"] = "Məbləğ 0-dan böyük olmalıdır.";
                return RedirectToReferrerOrIndex();
            }
            var user = await _userManager.GetUserAsync(User);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestPayments = HttpContext.Session.GetObject<List<UpcomingPayment>>("Guest_UpcomingPayments")
                                     ?? new List<UpcomingPayment>();
                model.Id = guestPayments.Any() ? guestPayments.Max(p => p.Id) + 1 : 1;
                model.IsPaid = false;
                model.Currency = NormalizeCurrency(model.Currency);
                // Təkrarlanma varsa avtomatik true olsun
                model.IsRecurring = model.RecurrenceType != RecurrenceType.None;
                guestPayments.Add(model);
                HttpContext.Session.SetObject("Guest_UpcomingPayments", guestPayments);
                TempData["Success"] = "Qarşıdan gələn ödəniş sınaq rejimində (Session) əlavə edildi!";
                return RedirectToReferrerOrIndex();
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            if (ModelState.IsValid)
            {
                model.UserId = user.Id;
                model.IsPaid = false;
                model.Currency = NormalizeCurrency(model.Currency);
                // Təkrarlanma varsa avtomatik true olsun
                model.IsRecurring = model.RecurrenceType != RecurrenceType.None;
                _context.UpcomingPayments.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Qarşıdan gələn ödəniş uğurla əlavə edildi!";
            }
            else
            {
                TempData["Error"] = "Məlumatları düzgün daxil etdiyinizdən əmin olun.";
            }
            return RedirectToReferrerOrIndex();
        }
        // ==========================================
        // POST: /UpcomingPayment/MarkAsPaid
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id, string paymentMethod)
        {
            var user = await _userManager.GetUserAsync(User);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestPayments = HttpContext.Session.GetObject<List<UpcomingPayment>>("Guest_UpcomingPayments") ?? new List<UpcomingPayment>();
                var paymentGuest = guestPayments.FirstOrDefault(u => u.Id == id);
                if (paymentGuest != null && !paymentGuest.IsPaid)
                {
                    var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                    var paymentCurrencyGuest = NormalizeCurrency(paymentGuest.Currency);
                    int? cardIdForTransaction = null;
                    if (paymentMethod != "cash" && int.TryParse(paymentMethod, out int cardIdGuest))
                    {
                        var card = guestCards.FirstOrDefault(c => c.Id == cardIdGuest);
                        if (card == null)
                        {
                            TempData["Error"] = "Seçilmiş kart tapılmadı!";
                            return RedirectToReferrerOrIndex();
                        }
                        var cardCurrency = NormalizeCurrency(card.Currency);
                        decimal amountToSubtract = await _currencyService.ConvertAsync(paymentGuest.Amount, paymentCurrencyGuest, cardCurrency);
                        if (card.Balance < amountToSubtract)
                        {
                            TempData["Error"] = $"Seçilmiş kartda kifayət qədər vəsait yoxdur! (Balans: {card.Balance:N2} {cardCurrency})";
                            return RedirectToReferrerOrIndex();
                        }
                        card.Balance -= amountToSubtract;
                        cardIdForTransaction = card.Id;
                        HttpContext.Session.SetObject("Guest_Cards", guestCards);
                    }
                    // Əməliyyatlar siyahısına xərc kimi əlavə olunur
                    var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                    int nextTrId = guestTransactions.Any() ? guestTransactions.Max(t => t.Id) + 1 : 1;
                    guestTransactions.Add(new Transaction
                    {
                        Id = nextTrId,
                        Amount = paymentGuest.Amount,
                        Currency = paymentCurrencyGuest,
                        Date = DateTime.Now,
                        Description = $"{paymentGuest.Title} (Avtomatik)",
                        IsIncome = false,
                        Status = "Tamamlandı",
                        CardId = cardIdForTransaction,
                        Category = new Category { Name = "Xərclər" }
                    });
                    HttpContext.Session.SetObject("Guest_Transactions", guestTransactions);
                    paymentGuest.IsPaid = true;
                    HttpContext.Session.SetObject("Guest_UpcomingPayments", guestPayments);
                    TempData["Success"] = "Ödəniş sınaq rejimində uğurla icra olundu!";
                }
                else
                {
                    TempData["Error"] = "Ödəniş tapılmadı və ya artıq ödənilib.";
                }
                return RedirectToReferrerOrIndex();
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var payment = await _context.UpcomingPayments
                .FirstOrDefaultAsync(u => u.Id == id && u.UserId == user.Id);
            if (payment == null || payment.IsPaid)
            {
                TempData["Error"] = "Ödəniş tapılmadı və ya artıq icra edilib!";
                return RedirectToReferrerOrIndex();
            }
            var paymentCurrency = NormalizeCurrency(payment.Currency);
            Card? selectedCard = null;
            // --- BALANS YOXLANIŞI (VALIDATION) ---
            if (paymentMethod != "cash" && int.TryParse(paymentMethod, out int selectedCardId))
            {
                selectedCard = await _context.Cards
                    .FirstOrDefaultAsync(c => c.Id == selectedCardId && c.UserId == user.Id && !c.IsDeleted);
                if (selectedCard == null)
                {
                    TempData["Error"] = "Seçilmiş kart tapılmadı və ya silinib!";
                    return RedirectToReferrerOrIndex();
                }
                var cardCurrency = NormalizeCurrency(selectedCard.Currency);
                decimal amountToCheck = await _currencyService.ConvertAsync(payment.Amount, paymentCurrency, cardCurrency);
                if (selectedCard.Balance < amountToCheck)
                {
                    TempData["Error"] = $"Seçilmiş kartda kifayət qədər vəsait yoxdur! (Balans: {selectedCard.Balance:N2} {cardCurrency})";
                    return RedirectToReferrerOrIndex();
                }
            }
            else if (paymentMethod == "cash")
            {
                decimal amountInAzn = await _currencyService.ConvertAsync(payment.Amount, paymentCurrency, "AZN");

                if (user.CashBalance < amountInAzn)
                {
                    TempData["Error"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Mövcud Nağd: {user.CashBalance:N2} AZN)";
                    return RedirectToReferrerOrIndex();
                }
            }
            // --- ATOMIC TRANSACTION BAŞLADIRIQ ---
            using var databaseTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var defaultCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Type == "Expense" && (c.UserId == user.Id || c.UserId == null) && !c.IsDeleted);

                if (defaultCategory == null)
                {
                    defaultCategory = new Category
                    {
                        Name = "Xərclər",
                        Type = "Expense",
                        Icon = "default.png",
                        UserId = user.Id
                    };
                    _context.Categories.Add(defaultCategory);
                    await _context.SaveChangesAsync();
                }
                payment.IsPaid = true;
                // Təkrarlanma intervalına görə növbəti tarixi hesabla
                switch (payment.RecurrenceType)
                {
                    case RecurrenceType.Daily:
                        payment.DueDate = payment.DueDate.AddDays(1);
                        payment.IsPaid = false;
                        break;
                    case RecurrenceType.Weekly:
                        payment.DueDate = payment.DueDate.AddDays(7);
                        payment.IsPaid = false;
                        break;
                    case RecurrenceType.Monthly:
                        payment.DueDate = payment.DueDate.AddMonths(1);
                        payment.IsPaid = false;
                        break;
                    case RecurrenceType.Yearly:
                        payment.DueDate = payment.DueDate.AddYears(1);
                        payment.IsPaid = false;
                        break;
                    case RecurrenceType.None:
                    default:
                        payment.IsPaid = true;
                        break;
                }
                var newExpense = new Transaction
                {
                    UserId = user.Id,
                    Amount = payment.Amount,
                    Currency = paymentCurrency,
                    Date = DateTime.Now,
                    Description = $"{payment.Title} (Avtomatik)",
                    IsIncome = false,
                    Status = "Tamamlandı",
                    CategoryId = defaultCategory.Id
                };
                if (selectedCard != null)
                {
                    var cardCurrency = NormalizeCurrency(selectedCard.Currency);
                    decimal amountToSubtract = await _currencyService.ConvertAsync(payment.Amount, paymentCurrency, cardCurrency);
                    newExpense.CardId = selectedCard.Id;
                    selectedCard.Balance -= amountToSubtract;
                    decimal totalBalanceSubtract = await _currencyService.ConvertAsync(payment.Amount, paymentCurrency, "AZN");
                    user.TotalBalance -= totalBalanceSubtract;
                }
                else if (paymentMethod == "cash")
                {
                    decimal amountToSubtractAzn = await _currencyService.ConvertAsync(payment.Amount, paymentCurrency, "AZN");
                    user.CashBalance -= amountToSubtractAzn;
                    user.TotalBalance -= amountToSubtractAzn;
                }
                _context.Transactions.Add(newExpense);
                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();
                TempData["Success"] = "Ödəniş uğurla həyata keçirildi!";
            }
            catch (Exception ex)
            {
                await databaseTransaction.RollbackAsync();
                _logger.LogError(ex, "Upcoming payment xətası baş verdi. PaymentId: {PaymentId}, UserId: {UserId}", id, user.Id);
                TempData["Error"] = "Ödəniş icra edilərkən gözlənilməz xəta baş verdi.";
            }
            return RedirectToReferrerOrIndex();
        }
        // ==========================================
        // POST: /UpcomingPayment/Delete/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestPayments = HttpContext.Session.GetObject<List<UpcomingPayment>>("Guest_UpcomingPayments") ?? new List<UpcomingPayment>();
                var paymentGuest = guestPayments.FirstOrDefault(u => u.Id == id);
                if (paymentGuest != null)
                {
                    guestPayments.Remove(paymentGuest);
                    HttpContext.Session.SetObject("Guest_UpcomingPayments", guestPayments);
                    TempData["Success"] = "Ödəniş sınaq rejimində silindi.";
                }
                else
                {
                    TempData["Error"] = "Ödəniş tapılmadı.";
                }
                return RedirectToReferrerOrIndex();
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var payment = await _context.UpcomingPayments
                .FirstOrDefaultAsync(u => u.Id == id && u.UserId == user.Id);
            if (payment != null)
            {
                _context.UpcomingPayments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ödəniş silindi.";
            }
            else
            {
                TempData["Error"] = "Ödəniş tapılmadı və ya sizə aid deyil.";
            }
            return RedirectToReferrerOrIndex();
        }
    }
}