using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace budget_planner.Controllers
{
    public class CardController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CurrencyService _currencyService; // Canlı CBAR məzənnə servisi

        public CardController(
            BudgetDbContext context,
            UserManager<ApplicationUser> userManager,
            CurrencyService currencyService)
        {
            _context = context;
            _userManager = userManager;
            _currencyService = currencyService;
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

            // 1. HƏLL: Qonaq istifadəçi üçün səssiz imtinanın qarşısı alındı
            if (user == null)
            {
                TempData["SuccessMessage"] = "Kart sınaq rejimində əlavə olundu! Saytdan çıxdıqda məlumatlar sıfırlanacaq.";
                return RedirectToAction("Index", "Home");
            }

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

            // 2. HƏLL: NullReferenceException xətasının qarşısı alındı
            if (user == null)
            {
                TempData["SuccessMessage"] = "Köçürmə sınaq rejimində icra olundu! Saytdan çıxdıqda məlumatlar sıfırlanacaq.";
                return RedirectToAction("Index", "Home");
            }

            var fromCard = await _context.Cards.FindAsync(model.FromCardId);
            var toCard = await _context.Cards.FindAsync(model.ToCardId);

            if (fromCard != null && toCard != null && !fromCard.IsDeleted && !toCard.IsDeleted)
            {
                // Modaldan seçilən valyuta (boşdursa göndərən kartın valyutası götürülür)
                string selectedCurrency = !string.IsNullOrWhiteSpace(model.Currency) ? model.Currency : (fromCard.Currency ?? "AZN");
                string fromCardCurrency = fromCard.Currency ?? "AZN";
                string toCardCurrency = toCard.Currency ?? "AZN";

                // 1. Göndərən kartın valyutasına konvertasiya (məsələn: Modalda USD seçilib, kart AZN-dir)
                decimal rateFrom = await GetExchangeRateAsync(selectedCurrency, fromCardCurrency);
                decimal amountDeductedFromCard = model.Amount * rateFrom;

                // Göndərən kartın balansını yoxlayırıq (Karta uyğun konvertasiya olunmuş məbləğlə)
                if (fromCard.Balance < amountDeductedFromCard)
                {
                    TempData["ErrorMessage"] = $"Göndərən kartda kifayət qədər vəsait yoxdur! (Kartdan çıxılacaq məbləğ: {amountDeductedFromCard:N2} {fromCardCurrency})";
                    return RedirectToAction("Index", "Home");
                }

                // 2. Alan kartın valyutasına konvertasiya (məsələn: Modalda USD seçilib, alan kart EUR-dur)
                decimal rateTo = await GetExchangeRateAsync(selectedCurrency, toCardCurrency);
                decimal amountAddedToCard = model.Amount * rateTo;

                // Balansların yenilənməsi
                fromCard.Balance -= amountDeductedFromCard;
                toCard.Balance += amountAddedToCard;

                var category = _context.Categories.FirstOrDefault(c => c.Name == "Transfer") ?? _context.Categories.FirstOrDefault();

                // Göndərən kart üçün Tranzaksiya (Göndərən kartın valyutası və məbləği ilə)
                _context.Transactions.Add(new Transaction
                {
                    CardId = model.FromCardId,
                    Amount = amountDeductedFromCard,
                    Description = $"Transfer -> {toCard.CardName} ({model.Amount:N2} {selectedCurrency})",
                    IsIncome = false,
                    Date = DateTime.Now,
                    Currency = fromCardCurrency,
                    UserId = user!.Id,
                    CategoryId = category!.Id,
                    Status = "Tamamlandı"
                });

                // Alan kart üçün Tranzaksiya (Alan kartın valyutası və məbləği ilə)
                _context.Transactions.Add(new Transaction
                {
                    CardId = model.ToCardId,
                    Amount = amountAddedToCard,
                    Description = $"Transfer <- {fromCard.CardName} ({model.Amount:N2} {selectedCurrency})",
                    IsIncome = true,
                    Date = DateTime.Now,
                    Currency = toCardCurrency,
                    UserId = user!.Id,
                    CategoryId = category!.Id,
                    Status = "Tamamlandı"
                });

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Köçürmə uğurla həyata keçirildi! Kartınızdan {amountDeductedFromCard:N2} {fromCardCurrency} çıxıldı və qarşı tərəfə {amountAddedToCard:N2} {toCardCurrency} otuzduruldu.";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        // [Authorize] <-- 3. HƏLL (1-ci hissə): Atribut silindi ki, login səhifəsinə məcbur etməsin
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            // 3. HƏLL (2-ci hissə): Login-ə yönləndirmək əvəzinə sınaq rejimi bildirişi verilir
            if (user == null)
            {
                TempData["SuccessMessage"] = "Kart sınaq rejimində silindi!";
                return RedirectToAction("Index", "Home");
            }

            var card = await _context.Cards
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id && !c.IsDeleted);

            if (card == null)
            {
                TempData["ErrorMessage"] = "Kart tapılmadı və ya artıq silinib!";
                return RedirectToAction("Index", "Home");
            }

            card.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Kart uğurla silindi!";
            return RedirectToAction("Index", "Home");
        }

        // TransactionController ilə eyni CBAR canlı məzənnə köməkçi funksiyası
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

            return fromRateInAzn / toRateInAzn;
        }
    }
}