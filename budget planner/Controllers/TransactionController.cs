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
    public class TransactionController : Controller
    {
        private readonly BudgetDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrencyService _currencyService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            BudgetDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrencyService currencyService,
            ICategoryService categoryService,
            ILogger<TransactionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _currencyService = currencyService;
            _categoryService = categoryService;
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
        // GET: /Transaction/Index
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions")
                                           ?? new List<Transaction>();
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards")
                                  ?? new List<Card>();

                ViewBag.Cards = guestCards;
                ViewBag.Categories = new List<Category>();

                var guestVmList = guestTransactions
                    .OrderByDescending(t => t.Date)
                    .Select(t => new TransactionVM
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Description = t.Description,
                        Date = t.Date,
                        IsIncome = t.IsIncome,
                        CategoryName = t.Category != null ? t.Category.Name : "Ümumi",
                        Currency = NormalizeCurrency(t.Currency),
                        CardId = t.CardId,
                        CardName = t.CardId.HasValue ? (guestCards.FirstOrDefault(c => c.Id == t.CardId.Value)?.CardName ?? "Nağd Pul") : "Nağd Pul",
                        Status = t.Status ?? "Tamamlandı"
                    }).ToList();

                return View(guestVmList);
            }

            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            ViewBag.Cards = await _context.Cards
                .Where(c => c.UserId == user.Id && !c.IsDeleted)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .Where(c => c.UserId == user.Id || c.UserId == null)
                .ToListAsync();

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.Card)
                .Where(t => t.UserId == user.Id && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .Select(t => new TransactionVM
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description,
                    Date = t.Date,
                    IsIncome = t.IsIncome,
                    CategoryName = t.Category != null ? t.Category.Name : "Ümumi",
                    Currency = NormalizeCurrency(t.Currency),
                    CardId = t.CardId,
                    CardName = t.Card != null ? t.Card.CardName : "Nağd",
                    Status = t.Status ?? "Tamamlandı"
                })
                .ToListAsync();

            return View(transactions);
        }

        // ==========================================
        // GET: /Transaction/Create
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // POST: /Transaction/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Məlumatlar düzgün daxil edilməyib.";
                return RedirectToReferrerOrIndex();
            }

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
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                var trCurrency = NormalizeCurrency(model.Currency);

                Card? selectedCard = null;

                if (model.CardId.HasValue && model.CardId.Value > 0)
                {
                    selectedCard = guestCards.FirstOrDefault(c => c.Id == model.CardId.Value);
                }

                // Əgər Kart Seçilibsə -> Bank Kartının Balansını Dəyişirik
                if (selectedCard != null)
                {
                    var cardCurrency = NormalizeCurrency(selectedCard.Currency);
                    var convertedAmount = await _currencyService.ConvertAsync(model.Amount, trCurrency, cardCurrency);

                    if (convertedAmount <= 0)
                    {
                        TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                        return RedirectToReferrerOrIndex();
                    }

                    if (!model.IsIncome && selectedCard.Balance < convertedAmount)
                    {
                        TempData["Error"] = $"Kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {convertedAmount:N2} {cardCurrency}, Mövcud: {selectedCard.Balance:N2} {cardCurrency})";
                        return RedirectToReferrerOrIndex();
                    }

                    if (model.IsIncome)
                        selectedCard.Balance += convertedAmount;
                    else
                        selectedCard.Balance -= convertedAmount;

                    HttpContext.Session.SetObject("Guest_Cards", guestCards);
                }
                // Əgər Kart Seçilməyibsə -> Yaşıl Kartın (Nağd Pul) Balansını Dəyişirik
                else
                {
                    var guestCashBalance = HttpContext.Session.GetObject<decimal?>("Guest_CashBalance") ?? 0m;
                    var convertedAmountAzn = await _currencyService.ConvertAsync(model.Amount, trCurrency, "AZN");

                    if (convertedAmountAzn <= 0)
                    {
                        TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                        return RedirectToReferrerOrIndex();
                    }

                    if (!model.IsIncome && guestCashBalance < convertedAmountAzn)
                    {
                        TempData["Error"] = $"Nağd balansda kifayət qədər vəsait yoxdur! (Tələb olunan: {convertedAmountAzn:N2} AZN, Mövcud Nağd: {guestCashBalance:N2} AZN)";
                        return RedirectToReferrerOrIndex();
                    }

                    if (model.IsIncome)
                        guestCashBalance += convertedAmountAzn;
                    else
                        guestCashBalance -= convertedAmountAzn;

                    HttpContext.Session.SetObject("Guest_CashBalance", guestCashBalance);
                }

                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions")
                                           ?? new List<Transaction>();

                var guestCategoryName = !string.IsNullOrWhiteSpace(model.NewCategoryName) ? model.NewCategoryName : "Ümumi";

                var guestTransaction = new Transaction
                {
                    Id = guestTransactions.Any() ? guestTransactions.Max(t => t.Id) + 1 : 1,
                    CardId = selectedCard?.Id, // Nağd pul seçildikdə CardId NULL olacaq
                    Card = null,
                    Amount = model.Amount,
                    Description = model.Description,
                    IsIncome = model.IsIncome,
                    Date = model.Date == default ? DateTime.Now : model.Date,
                    Currency = trCurrency,
                    Status = string.IsNullOrWhiteSpace(model.Status) ? "Tamamlandı" : model.Status,
                    Category = new Category { Name = guestCategoryName }
                };

                guestTransactions.Add(guestTransaction);
                HttpContext.Session.SetObject("Guest_Transactions", guestTransactions);

                TempData["SuccessMessage"] = "Əməliyyat sınaq rejimində (Session) əlavə olundu!";
                return RedirectToReferrerOrIndex();
            }

            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var transactionCurrency = NormalizeCurrency(model.Currency);

            Card? card = null;
            decimal convertedAmountSql = 0;
            decimal amountInAzn = 0;

            if (model.CardId.HasValue && model.CardId.Value > 0)
            {
                card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == model.CardId && c.UserId == user.Id && !c.IsDeleted);

                if (card == null)
                {
                    TempData["Error"] = "Seçilmiş kart tapılmadı və ya silinib!";
                    return RedirectToReferrerOrIndex();
                }

                var cardCurrency = NormalizeCurrency(card.Currency);
                convertedAmountSql = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, cardCurrency);

                if (convertedAmountSql <= 0)
                {
                    TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu və ya məzənnə xətası baş verdi.";
                    return RedirectToReferrerOrIndex();
                }

                if (!model.IsIncome && card.Balance < convertedAmountSql)
                {
                    TempData["Error"] = $"Kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {convertedAmountSql:N2} {cardCurrency})";
                    return RedirectToReferrerOrIndex();
                }

                amountInAzn = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, "AZN");
                if (amountInAzn <= 0)
                {
                    TempData["Error"] = "AZN ekvivalentinin hesablanması zamanı xəta baş verdi.";
                    return RedirectToReferrerOrIndex();
                }
            }
            else
            {
                amountInAzn = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, "AZN");

                if (amountInAzn <= 0)
                {
                    TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                    return RedirectToReferrerOrIndex();
                }

                if (!model.IsIncome && user.CashBalance < amountInAzn)
                {
                    TempData["Error"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Tələb olunan: {amountInAzn:N2} AZN, Mövcud Nağd: {user.CashBalance:N2} AZN)";
                    return RedirectToReferrerOrIndex();
                }
            }

            using var databaseTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var finalCategoryName = string.Empty;

                if (!string.IsNullOrWhiteSpace(model.NewCategoryName))
                {
                    finalCategoryName = model.NewCategoryName.Trim();
                }
                else if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
                {
                    var catDb = await _context.Categories.FirstOrDefaultAsync(c =>
                        c.Id == model.CategoryId.Value &&
                        (c.UserId == user.Id || c.UserId == null));

                    if (catDb != null)
                    {
                        finalCategoryName = catDb.Name;
                    }
                }

                if (string.IsNullOrWhiteSpace(finalCategoryName))
                {
                    finalCategoryName = "Ümumi";
                }

                var category = await _categoryService.GetOrCreateAsync(finalCategoryName, model.IsIncome, user.Id);

                if (category == null)
                {
                    await databaseTransaction.RollbackAsync();
                    TempData["Error"] = "Kateqoriya yaradılarkən xəta baş verdi.";
                    return RedirectToReferrerOrIndex();
                }

                if (card != null)
                {
                    if (model.IsIncome)
                    {
                        card.Balance += convertedAmountSql;
                        user.TotalBalance += amountInAzn;
                    }
                    else
                    {
                        card.Balance -= convertedAmountSql;
                        user.TotalBalance -= amountInAzn;
                    }
                }
                else
                {
                    if (model.IsIncome)
                    {
                        user.CashBalance += amountInAzn;
                        user.TotalBalance += amountInAzn;
                    }
                    else
                    {
                        user.CashBalance -= amountInAzn;
                        user.TotalBalance -= amountInAzn;
                    }
                }

                var transaction = new Transaction
                {
                    CardId = card?.Id,
                    Amount = model.Amount,
                    Description = model.Description,
                    IsIncome = model.IsIncome,
                    Date = model.Date == default ? DateTime.Now : model.Date,
                    Currency = transactionCurrency,
                    UserId = user.Id,
                    Category = category,
                    Status = string.IsNullOrWhiteSpace(model.Status) ? "Tamamlandı" : model.Status
                };

                _context.Transactions.Add(transaction);
                _context.Users.Update(user);

                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();

                TempData["SuccessMessage"] = "Əməliyyat uğurla qeydə alındı!";
                return RedirectToReferrerOrIndex();
            }
            catch (Exception ex)
            {
                await databaseTransaction.RollbackAsync();
                _logger.LogError(ex, "Transaction Create əməliyyatı zamanı xəta baş verdi. İstifadəçi ID: {UserId}", user.Id);

                TempData["Error"] = "Əməliyyat yerinə yetirilərkən texniki xəta baş verdi. Zəhmət olmasa yenidən cəhd edin.";
                return RedirectToReferrerOrIndex();
            }
        }

        // ==========================================
        // POST: /Transaction/Delete/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                var trToDelete = guestTransactions.FirstOrDefault(t => t.Id == id);

                if (trToDelete != null)
                {
                    var trCurrency = NormalizeCurrency(trToDelete.Currency);

                    // Əgər Kart Əməliyyatıdırsa -> Kart Balansını Bərpa Et
                    if (trToDelete.CardId.HasValue && trToDelete.CardId.Value > 0)
                    {
                        var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                        var card = guestCards.FirstOrDefault(c => c.Id == trToDelete.CardId.Value);

                        if (card != null)
                        {
                            var cardCurrency = NormalizeCurrency(card.Currency);
                            var convertedAmount = await _currencyService.ConvertAsync(trToDelete.Amount, trCurrency, cardCurrency);

                            if (convertedAmount <= 0)
                            {
                                TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                                return RedirectToReferrerOrIndex();
                            }

                            if (trToDelete.IsIncome)
                            {
                                if (card.Balance < convertedAmount)
                                {
                                    TempData["Error"] = "Bu gəlir əməliyyatı silinə bilməz. Balans kifayət etmir!";
                                    return RedirectToReferrerOrIndex();
                                }
                                card.Balance -= convertedAmount;
                            }
                            else
                            {
                                card.Balance += convertedAmount;
                            }

                            HttpContext.Session.SetObject("Guest_Cards", guestCards);
                        }
                    }
                    // Əgər Nağd Pul Əməliyyatıdırsa -> Nağd Balansı Bərpa Et
                    else
                    {
                        var guestCashBalance = HttpContext.Session.GetObject<decimal?>("Guest_CashBalance") ?? 0m;
                        var convertedAmountAzn = await _currencyService.ConvertAsync(trToDelete.Amount, trCurrency, "AZN");

                        if (convertedAmountAzn <= 0)
                        {
                            TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                            return RedirectToReferrerOrIndex();
                        }

                        if (trToDelete.IsIncome)
                        {
                            if (guestCashBalance < convertedAmountAzn)
                            {
                                TempData["Error"] = "Bu gəlir əməliyyatı silinə bilməz. Nağd balans kifayət etmir!";
                                return RedirectToReferrerOrIndex();
                            }
                            guestCashBalance -= convertedAmountAzn;
                        }
                        else
                        {
                            guestCashBalance += convertedAmountAzn;
                        }

                        HttpContext.Session.SetObject("Guest_CashBalance", guestCashBalance);
                    }

                    guestTransactions.Remove(trToDelete);
                    HttpContext.Session.SetObject("Guest_Transactions", guestTransactions);
                    TempData["SuccessMessage"] = "Əməliyyat silindi və balans bərpa olundu!";
                }
                else
                {
                    TempData["Error"] = "Əməliyyat tapılmadı.";
                }

                return RedirectToReferrerOrIndex();
            }

            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var transaction = await _context.Transactions
                .Include(t => t.Card)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id && !t.IsDeleted);

            if (transaction == null)
            {
                TempData["Error"] = "Əməliyyat tapılmadı və ya sizə aid deyil.";
                return RedirectToReferrerOrIndex();
            }

            using var databaseTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trCurrency = NormalizeCurrency(transaction.Currency);
                var amountInAzn = await _currencyService.ConvertAsync(transaction.Amount, trCurrency, "AZN");

                if (amountInAzn <= 0)
                {
                    await databaseTransaction.RollbackAsync();
                    TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                    return RedirectToReferrerOrIndex();
                }

                // Təhlükəsiz Kart/Nağd ayrımı (Aşkar Rollback-lər ilə)
                if (transaction.CardId.HasValue)
                {
                    if (transaction.Card == null)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = "Əməliyyatın aid olduğu kart tapılmadı və ya silinib.";
                        return RedirectToReferrerOrIndex();
                    }

                    var card = transaction.Card;
                    var cardCurrency = NormalizeCurrency(card.Currency);
                    var convertedAmountCard = await _currencyService.ConvertAsync(transaction.Amount, trCurrency, cardCurrency);

                    if (convertedAmountCard <= 0)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                        return RedirectToReferrerOrIndex();
                    }

                    if (transaction.IsIncome)
                    {
                        if (card.Balance < convertedAmountCard)
                        {
                            await databaseTransaction.RollbackAsync();
                            TempData["Error"] = "Bu əməliyyat silinə bilməz. Kart balansı kifayət etmir!";
                            return RedirectToReferrerOrIndex();
                        }

                        card.Balance -= convertedAmountCard;
                        user.TotalBalance -= amountInAzn;
                    }
                    else
                    {
                        card.Balance += convertedAmountCard;
                        user.TotalBalance += amountInAzn;
                    }
                }
                else
                {
                    if (transaction.IsIncome)
                    {
                        if (user.CashBalance < amountInAzn)
                        {
                            await databaseTransaction.RollbackAsync();
                            TempData["Error"] = "Bu əməliyyat silinə bilməz. Nağd balans kifayət etmir!";
                            return RedirectToReferrerOrIndex();
                        }

                        user.CashBalance -= amountInAzn;
                        user.TotalBalance -= amountInAzn;
                    }
                    else
                    {
                        user.CashBalance += amountInAzn;
                        user.TotalBalance += amountInAzn;
                    }
                }

                // Soft Delete
                transaction.IsDeleted = true;

                _context.Users.Update(user);

                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();

                TempData["SuccessMessage"] = "Əməliyyat silindi və balans bərpa olundu!";
                return RedirectToReferrerOrIndex();
            }
            catch (Exception ex)
            {
                await databaseTransaction.RollbackAsync();
                _logger.LogError(ex, "Transaction Delete əməliyyatı zamanı xəta baş verdi. Transaction ID: {TransactionId}, İstifadəçi ID: {UserId}", id, user.Id);

                TempData["Error"] = "Əməliyyat silinərkən texniki xəta baş verdi.";
                return RedirectToReferrerOrIndex();
            }
        }
    }
}