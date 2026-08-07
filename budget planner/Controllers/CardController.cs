using budget_planner.DAL;
using budget_planner.Extensions;
using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels;
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
    public class CardController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<CardController> _logger;
        public CardController(
            BudgetDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrencyService currencyService,
            ILogger<CardController> logger)
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
        // Köməkçi Metod: Əməliyyatın gəldiyi səhifəyə və ya Home-a yönləndirir
        private IActionResult RedirectToReferrerOrHome()
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
            return RedirectToAction("Index", "Home");
        }
        // ==========================================
        // POST: /Card/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CardCreateVM model)
        {
            // 🟢 QƏPİK / VERGÜL DÜZƏLİŞİ (ƏLAVƏ EDİLDİ)
            string rawBalance = Request.Form["Balance"].ToString();
            if (!string.IsNullOrWhiteSpace(rawBalance))
            {
                rawBalance = rawBalance.Replace(',', '.');
                if (decimal.TryParse(rawBalance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal validBalance))
                {
                    model.Balance = validBalance;
                    ModelState.Remove("Balance");
                }
            }
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToReferrerOrHome();
            }
            var user = await _userManager.GetUserAsync(User);
            var cardCurrency = NormalizeCurrency(model.Currency);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                int newId = guestCards.Any() ? guestCards.Max(c => c.Id) + 1 : 1;
                var newGuestCard = new Card
                {
                    Id = newId,
                    CardName = model.CardName,
                    Last4Digits = model.Last4Digits,
                    Currency = cardCurrency,
                    Balance = model.Balance
                };
                guestCards.Add(newGuestCard);
                HttpContext.Session.SetObject("Guest_Cards", guestCards);
                TempData["SuccessMessage"] = "Kart sınaq rejimində (Session) əlavə olundu!";
                return RedirectToReferrerOrHome();
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newCard = new Card
                {
                    CardName = model.CardName,
                    Last4Digits = model.Last4Digits,
                    Currency = cardCurrency,
                    Balance = model.Balance,
                    UserId = user.Id
                };
                _context.Cards.Add(newCard);
                // Əgər kartın ilkin balansı varsa, istifadəçinin TotalBalance-nə AZN ekvivalentini əlavə edirik
                if (model.Balance != 0)
                {
                    decimal amountInAzn = await _currencyService.ConvertAsync(model.Balance, cardCurrency, "AZN");
                    user.TotalBalance += amountInAzn;
                }
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                TempData["SuccessMessage"] = "Kart uğurla əlavə edildi!";
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Kart yaradılarkən xəta baş verdi. UserId: {UserId}", user.Id);
                TempData["ErrorMessage"] = "Kart əlavə edilərkən texniki xəta baş verdi.";
            }
            return RedirectToReferrerOrHome();
        }
        // ==========================================
        // POST: /Card/Transfer
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(TransferVM model)
        {
            // 🟢 QƏPİK / VERGÜL DÜZƏLİŞİ (ƏLAVƏ EDİLDİ)
            string rawAmount = Request.Form["Amount"].ToString();
            if (!string.IsNullOrWhiteSpace(rawAmount))
            {
                rawAmount = rawAmount.Replace(',', '.');
                if (decimal.TryParse(rawAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal validAmount))
                {
                    model.Amount = validAmount;
                    ModelState.Remove("Amount");
                }
            }
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToReferrerOrHome();
            }
            int fromId = model.FromCardId;
            int toId = model.ToCardId;
            if (fromId == toId)
            {
                TempData["ErrorMessage"] = "Göndərən və alan hesab eyni ola bilməz!";
                return RedirectToReferrerOrHome();
            }
            if (model.Amount <= 0)
            {
                TempData["ErrorMessage"] = "Məbləğ 0-dan böyük olmalıdır!";
                return RedirectToReferrerOrHome();
            }
            var user = await _userManager.GetUserAsync(User);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                var guestCashBalance = HttpContext.Session.GetObject<decimal?>("Guest_CashBalance") ?? 0m;
                Card? fromGuestCard = fromId > 0 ? guestCards.FirstOrDefault(c => c.Id == fromId) : null;
                Card? toGuestCard = toId > 0 ? guestCards.FirstOrDefault(c => c.Id == toId) : null;
                if (fromId > 0 && fromGuestCard == null)
                {
                    TempData["ErrorMessage"] = "Göndərən kart tapılmadı!";
                    return RedirectToReferrerOrHome();
                }
                if (toId > 0 && toGuestCard == null)
                {
                    TempData["ErrorMessage"] = "Alan kart tapılmadı!";
                    return RedirectToReferrerOrHome();
                }
                string fromName = fromGuestCard != null ? fromGuestCard.CardName : "Nağd Pul";
                string toName = toGuestCard != null ? toGuestCard.CardName : "Nağd Pul";
                string fromCurrency = fromGuestCard != null ? NormalizeCurrency(fromGuestCard.Currency) : "AZN";
                string toCurrency = toGuestCard != null ? NormalizeCurrency(toGuestCard.Currency) : "AZN";
                string selectedCurrency = !string.IsNullOrWhiteSpace(model.Currency)
                    ? model.Currency.Trim().ToUpper()
                    : fromCurrency;
                decimal amountDeducted = await _currencyService.ConvertAsync(model.Amount, selectedCurrency, fromCurrency);
                decimal amountAdded = await _currencyService.ConvertAsync(model.Amount, selectedCurrency, toCurrency);
                if (amountDeducted <= 0 || amountAdded <= 0)
                {
                    TempData["ErrorMessage"] = "Valyuta çevrilməsi uğursuz oldu!";
                    return RedirectToReferrerOrHome();
                }
                // Balans Yoxlanışı (Göndərən Tərəf)
                if (fromGuestCard != null)
                {
                    if (fromGuestCard.Balance < amountDeducted)
                    {
                        TempData["ErrorMessage"] = $"Göndərən kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {amountDeducted:N2} {fromCurrency})";
                        return RedirectToReferrerOrHome();
                    }
                    fromGuestCard.Balance -= amountDeducted;
                }
                else
                {
                    if (guestCashBalance < amountDeducted)
                    {
                        TempData["ErrorMessage"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Tələb olunan: {amountDeducted:N2} AZN)";
                        return RedirectToReferrerOrHome();
                    }
                    guestCashBalance -= amountDeducted;
                }
                // Balans Əlavəsi (Alan Tərəf)
                if (toGuestCard != null)
                {
                    toGuestCard.Balance += amountAdded;
                }
                else
                {
                    guestCashBalance += amountAdded;
                }
                HttpContext.Session.SetObject("Guest_Cards", guestCards);
                HttpContext.Session.SetObject("Guest_CashBalance", guestCashBalance);
                // Transfer Əməliyyat Tarixçəsi
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                int nextId = guestTransactions.Any() ? guestTransactions.Max(t => t.Id) + 1 : 1;
                guestTransactions.Add(new Transaction
                {
                    Id = nextId++,
                    CardId = fromGuestCard?.Id,
                    Amount = amountDeducted,
                    Description = $"Transfer -> {toName} ({model.Amount:N2} {selectedCurrency})",
                    IsIncome = false,
                    Date = DateTime.Now,
                    Currency = fromCurrency,
                    Status = "Tamamlandı",
                    Category = new Category { Name = "Transfer" }
                });

                guestTransactions.Add(new Transaction
                {
                    Id = nextId,
                    CardId = toGuestCard?.Id,
                    Amount = amountAdded,
                    Description = $"Transfer <- {fromName} ({model.Amount:N2} {selectedCurrency})",
                    IsIncome = true,
                    Date = DateTime.Now,
                    Currency = toCurrency,
                    Status = "Tamamlandı",
                    Category = new Category { Name = "Transfer" }
                });
                HttpContext.Session.SetObject("Guest_Transactions", guestTransactions);
                TempData["SuccessMessage"] = "Köçürmə sınaq rejimində uğurla icra olundu!";
                return RedirectToReferrerOrHome();
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            Card? fromCard = fromId > 0
                ? await _context.Cards.FirstOrDefaultAsync(c => c.Id == fromId && c.UserId == user.Id && !c.IsDeleted)
                : null;
            Card? toCard = toId > 0
                ? await _context.Cards.FirstOrDefaultAsync(c => c.Id == toId && c.UserId == user.Id && !c.IsDeleted)
                : null;
            if (fromId > 0 && fromCard == null)
            {
                TempData["ErrorMessage"] = "Göndərən kart tapılmadı və ya sizə aid deyil!";
                return RedirectToReferrerOrHome();
            }
            if (toId > 0 && toCard == null)
            {
                TempData["ErrorMessage"] = "Alan kart tapılmadı və ya sizə aid deyil!";
                return RedirectToReferrerOrHome();
            }
            string fromCardName = fromCard != null ? fromCard.CardName : "Nağd Pul";
            string toCardName = toCard != null ? toCard.CardName : "Nağd Pul";
            string fromCardCurrency = fromCard != null ? NormalizeCurrency(fromCard.Currency) : "AZN";
            string toCardCurrency = toCard != null ? NormalizeCurrency(toCard.Currency) : "AZN";
            string selectedCurr = !string.IsNullOrWhiteSpace(model.Currency)
                ? model.Currency.Trim().ToUpper()
                : fromCardCurrency;
            decimal deductedAmount = await _currencyService.ConvertAsync(model.Amount, selectedCurr, fromCardCurrency);
            decimal addedAmount = await _currencyService.ConvertAsync(model.Amount, selectedCurr, toCardCurrency);
            if (deductedAmount <= 0 || addedAmount <= 0)
            {
                TempData["ErrorMessage"] = "Valyuta çevrilməsi uğursuz oldu!";
                return RedirectToReferrerOrHome();
            }
            // Göndərən Balans Yoxlanışı
            if (fromCard != null)
            {
                if (fromCard.Balance < deductedAmount)
                {
                    TempData["ErrorMessage"] = $"Göndərən kartda kifayət qədər vəsait yoxdur! (Çıxılacaq məbləğ: {deductedAmount:N2} {fromCardCurrency})";
                    return RedirectToReferrerOrHome();
                }
            }
            else
            {
                if (user.CashBalance < deductedAmount)
                {
                    TempData["ErrorMessage"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Çıxılacaq məbləğ: {deductedAmount:N2} AZN)";
                    return RedirectToReferrerOrHome();
                }
            }
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kateqoriyanı tapmaq və ya yoxdursa avtomatik yaratmaq
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == "Transfer" && (c.UserId == user.Id || c.UserId == null) && !c.IsDeleted);
                if (category == null)
                {
                    category = new Category
                    {
                        Name = "Transfer",
                        UserId = user.Id
                    };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }
                // Balansları Yeniləmək
                if (fromCard != null)
                {
                    fromCard.Balance -= deductedAmount;
                }
                else
                {
                    user.CashBalance -= deductedAmount;
                }
                if (toCard != null)
                {
                    toCard.Balance += addedAmount;
                }
                else
                {
                    user.CashBalance += addedAmount;
                }
                // Tranzaksiyaları əlavə etmək
                _context.Transactions.Add(new Transaction
                {
                    CardId = fromCard?.Id,
                    Amount = deductedAmount,
                    Description = $"Transfer -> {toCardName} ({model.Amount:N2} {selectedCurr})",
                    IsIncome = false,
                    Date = DateTime.Now,
                    Currency = fromCardCurrency,
                    UserId = user.Id,
                    CategoryId = category.Id,
                    Status = "Tamamlandı"
                });
                _context.Transactions.Add(new Transaction
                {
                    CardId = toCard?.Id,
                    Amount = addedAmount,
                    Description = $"Transfer <- {fromCardName} ({model.Amount:N2} {selectedCurr})",
                    IsIncome = true,
                    Date = DateTime.Now,
                    Currency = toCardCurrency,
                    UserId = user.Id,
                    CategoryId = category.Id,
                    Status = "Tamamlandı"
                });
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                TempData["SuccessMessage"] = $"Köçürmə uğurla həyata keçirildi! {fromCardName} hesabınızdan {deductedAmount:N2} {fromCardCurrency} çıxıldı və {toCardName} hesabınıza {addedAmount:N2} {toCardCurrency} əlavə edildi.";
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Kartlararası transfer zamanı xəta baş verdi. FromCardId: {FromCardId}, ToCardId: {ToCardId}", model.FromCardId, model.ToCardId);
                TempData["ErrorMessage"] = "Köçürmə zamanı gözlənilməz xəta baş verdi!";
            }
            return RedirectToReferrerOrHome();
        }
        // ==========================================
        // POST: /Card/Delete/5
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
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                var guestCardToDelete = guestCards.FirstOrDefault(c => c.Id == id);
                if (guestCardToDelete != null)
                {
                    guestCards.Remove(guestCardToDelete);
                    HttpContext.Session.SetObject("Guest_Cards", guestCards);
                    TempData["SuccessMessage"] = "Kart sınaq rejimində silindi!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Kart tapılmadı!";
                }
                return RedirectToReferrerOrHome();
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var card = await _context.Cards
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id && !c.IsDeleted);
            if (card == null)
            {
                TempData["ErrorMessage"] = "Kart tapılmadı və ya artıq silinib!";
                return RedirectToReferrerOrHome();
            }
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kart silindikdə balansı varsa, istifadəçinin TotalBalance-dən AZN ekvivalentini çıxırıq
                if (card.Balance != 0)
                {
                    var cardCurrency = NormalizeCurrency(card.Currency);
                    decimal amountInAzn = await _currencyService.ConvertAsync(card.Balance, cardCurrency, "AZN");
                    user.TotalBalance -= amountInAzn;
                }
                // Soft Delete
                card.IsDeleted = true;
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                TempData["SuccessMessage"] = "Kart uğurla silindi!";
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Kart silinərkən xəta baş verdi. CardId: {CardId}, UserId: {UserId}", id, user.Id);
                TempData["ErrorMessage"] = "Kart silinərkən texniki xəta baş verdi.";
            }
            return RedirectToReferrerOrHome();
        }
        // ==========================================
        // HELPER: Valyuta Məzənnəsini Hesablayır
        // ==========================================
        private async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            if (fromCurrency == toCurrency) return 1.0m;
            var rates = await _currencyService.GetExchangeRatesAsync();
            decimal fromRateInAzn = fromCurrency == "AZN"
                ? 1.0m
                : rates.FirstOrDefault(r => r.Code == fromCurrency)?.Rate ?? 1.0m;
            decimal toRateInAzn = toCurrency == "AZN"
                ? 1.0m
                : rates.FirstOrDefault(r => r.Code == toCurrency)?.Rate ?? 1.0m;
            if (toRateInAzn <= 0) return 1.0m;
            return fromRateInAzn / toRateInAzn;
        }
    }
}