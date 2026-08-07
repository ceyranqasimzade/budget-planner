using budget_planner.DAL;
using budget_planner.Extensions;
using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels;
using Google.GenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // 1. IConfiguration üçün namespace əlavə edildi
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private readonly IConfiguration _configuration; // 2. Field elan olundu

        public TransactionController(
            BudgetDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrencyService currencyService,
            ICategoryService categoryService,
            ILogger<TransactionController> logger,
            IConfiguration configuration) // 3. Injection edildi
        {
            _context = context;
            _userManager = userManager;
            _currencyService = currencyService;
            _categoryService = categoryService;
            _logger = logger;
            _configuration = configuration; // 4. Mənimsədildi
        }

        // Köməkçi Metod: Valyuta kodunu standartlaşdırır (DRY Prinsipi)
        private static string NormalizeCurrency(string? currency)
        {
            return string.IsNullOrWhiteSpace(currency)
                ? "AZN"
                : currency.Trim().ToUpperInvariant();
        }

        // Köməkçi Metod: Sorğunun gəldiyi səhifəyə (Referer) və ya standart olaraq Index-ə yönləndirir
        private IActionResult RedirectToReferrerOrIndex()
        {
            string? referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrWhiteSpace(referer) &&
                Uri.TryCreate(referer, UriKind.Absolute, out var uri))
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
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                var guestCategories = HttpContext.Session.GetObject<List<Category>>("Guest_Categories") ?? new List<Category>();

                ViewBag.Cards = guestCards;
                ViewBag.Categories = guestCategories;

                var guestVmList = guestTransactions
                    .OrderByDescending(t => t.Date)
                    .Select(t => new TransactionVM
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Description = t.Description,
                        Date = t.Date,
                        IsIncome = t.IsIncome,
                        CategoryName = t.Category?.Name ?? "Ümumi",
                        Currency = NormalizeCurrency(t.Currency),
                        CardId = t.CardId,
                        CardName = t.CardId.HasValue
                            ? guestCards.FirstOrDefault(c => c.Id == t.CardId.Value)?.CardName ?? "Nağd Pul"
                            : "Nağd Pul",
                        Status = string.IsNullOrWhiteSpace(t.Status) ? "Tamamlandı" : t.Status
                    })
                    .ToList();

                return View(guestVmList);
            }

            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            ViewBag.Cards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.UserId == user.Id && !c.IsDeleted)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .Where(c => (c.UserId == user.Id || c.UserId == null) && !c.IsDeleted)
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
                    CardName = t.Card != null ? t.Card.CardName : "Nağd Pul",
                    Status = string.IsNullOrWhiteSpace(t.Status) ? "Tamamlandı" : t.Status
                })
                .ToListAsync();

            return View(transactions);
        }

        // ==========================================
        // KÖMƏKÇİ METODLAR
        // ==========================================
        private async Task PopulateCategoryAndCardDropDownsAsync(ApplicationUser? user)
        {
            if (user != null)
            {
                // 1. Öncə bazadan kartları gətiririk
                var userCards = await _context.Cards
                    .Where(c => c.UserId == user.Id && !c.IsDeleted)
                    .ToListAsync();
                // 2. Yaddaşda (Memory) SelectListItem formasına salırıq
                // (Əgər modeldə Name yerinə CardName-dırsa c.CardName yazırıq)
                ViewBag.Cards = userCards.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.CardName} — {c.Balance:N2} {c.Currency}" // <--- c.Name yerinə c.CardName (və ya sizdəki ad)
                }).ToList();

                var userCategories = await _context.Categories
                    .Where(c => c.UserId == user.Id || c.UserId == null)
                    .ToListAsync();
                ViewBag.Categories = userCategories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
            }
            else
            {
                // Qonaq (Session) rejimində olan kartlar üçün
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                ViewBag.Cards = guestCards.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.CardName} — {c.Balance:N2} {c.Currency}" // <--- c.Name yerinə c.CardName
                }).ToList();
                var guestCategories = HttpContext.Session.GetObject<List<Category>>("Guest_Categories") ?? new List<Category>();
                ViewBag.Categories = guestCategories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
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

                if (trToDelete == null)
                {
                    TempData["Error"] = "Əməliyyat tapılmadı.";
                    return RedirectToReferrerOrIndex();
                }
                var trCurrency = NormalizeCurrency(trToDelete.Currency);
                // Əgər Kart Əməliyyatıdırsa -> Kart Balansını Bərpa Et
                if (trToDelete.CardId.HasValue && trToDelete.CardId.Value > 0)
                {
                    var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                    var card = guestCards.FirstOrDefault(c => c.Id == trToDelete.CardId.Value);

                    if (card == null)
                    {
                        TempData["Error"] = "Əməliyyatın aid olduğu kart tapılmadı və ya silinib.";
                        return RedirectToReferrerOrIndex();
                    }
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
                            TempData["Error"] = "Bu gəlir əməliyyatı silinə bilməz. Kart balansı kifayət etmir!";
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
                // Təhlükəsiz Kart/Nağd ayrımı
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
        // ==========================================
        // POST: /Transaction/BulkDelete
        // ==========================================
        public class BulkDeleteRequest
        {
            public List<int> Ids { get; set; } = new List<int>();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
        {
            if (request == null || request.Ids == null || !request.Ids.Any())
            {
                return Json(new { success = false, message = "Silinmək üçün heç bir əməliyyat seçilməyib." });
            }
            var user = await _userManager.GetUserAsync(User);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                var guestCashBalance = HttpContext.Session.GetObject<decimal?>("Guest_CashBalance") ?? 0m;
                var itemsToDelete = guestTransactions.Where(t => request.Ids.Contains(t.Id)).ToList();
                if (!itemsToDelete.Any())
                {
                    return Json(new { success = false, message = "Seçilmiş əməliyyatlar tapılmadı." });
                }
                foreach (var trToDelete in itemsToDelete)
                {
                    var trCurrency = NormalizeCurrency(trToDelete.Currency);
                    // Kart Əməliyyatı
                    if (trToDelete.CardId.HasValue && trToDelete.CardId.Value > 0)
                    {
                        var card = guestCards.FirstOrDefault(c => c.Id == trToDelete.CardId.Value);
                        if (card == null)
                        {
                            return Json(new { success = false, message = $"'{trToDelete.Description}' əməliyyatının aid olduğu kart tapılmadı." });
                        }
                        var cardCurrency = NormalizeCurrency(card.Currency);
                        var convertedAmount = await _currencyService.ConvertAsync(trToDelete.Amount, trCurrency, cardCurrency);
                        if (convertedAmount <= 0)
                        {
                            return Json(new { success = false, message = $"'{trToDelete.Description}' üçün valyuta çevrilməsi uğursuz oldu." });
                        }
                        if (trToDelete.IsIncome)
                        {
                            if (card.Balance < convertedAmount)
                            {
                                return Json(new { success = false, message = $"'{trToDelete.Description}' gəlir əməliyyatı silinə bilməz. Kartda kifayət qədər balans yoxdur!" });
                            }
                            card.Balance -= convertedAmount;
                        }
                        else
                        {
                            card.Balance += convertedAmount;
                        }
                    }
                    // Nağd Pul Əməliyyatı
                    else
                    {
                        var convertedAmountAzn = await _currencyService.ConvertAsync(trToDelete.Amount, trCurrency, "AZN");
                        if (convertedAmountAzn <= 0)
                        {
                            return Json(new { success = false, message = $"'{trToDelete.Description}' üçün valyuta çevrilməsi uğursuz oldu." });
                        }
                        if (trToDelete.IsIncome)
                        {
                            if (guestCashBalance < convertedAmountAzn)
                            {
                                return Json(new { success = false, message = $"'{trToDelete.Description}' gəlir əməliyyatı silinə bilməz. Nağd balans kifayət etmir!" });
                            }
                            guestCashBalance -= convertedAmountAzn;
                        }
                        else
                        {
                            guestCashBalance += convertedAmountAzn;
                        }
                    }
                    guestTransactions.Remove(trToDelete);
                }
                // Sessiyaları yeniləyirik
                HttpContext.Session.SetObject("Guest_Transactions", guestTransactions);
                HttpContext.Session.SetObject("Guest_Cards", guestCards);
                HttpContext.Session.SetObject("Guest_CashBalance", guestCashBalance);
                return Json(new { success = true, message = $"{itemsToDelete.Count} əməliyyat uğurla silindi və balanslar bərpa olundu." });
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var transactions = await _context.Transactions
                .Include(t => t.Card)
                .Where(t => request.Ids.Contains(t.Id) && t.UserId == user.Id && !t.IsDeleted)
                .ToListAsync();
            if (!transactions.Any())
            {
                return Json(new { success = false, message = "Seçilmiş əməliyyatlar tapılmadı və ya sizə aid deyil." });
            }
            using var databaseTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var transaction in transactions)
                {
                    var trCurrency = NormalizeCurrency(transaction.Currency);
                    var amountInAzn = await _currencyService.ConvertAsync(transaction.Amount, trCurrency, "AZN");

                    if (amountInAzn <= 0)
                    {
                        await databaseTransaction.RollbackAsync();
                        return Json(new { success = false, message = $"'{transaction.Description}' üçün valyuta çevrilməsi uğursuz oldu." });
                    }
                    // Kart Əməliyyatı
                    if (transaction.CardId.HasValue)
                    {
                        if (transaction.Card == null)
                        {
                            await databaseTransaction.RollbackAsync();
                            return Json(new { success = false, message = $"'{transaction.Description}' əməliyyatının aid olduğu kart tapılmadı." });
                        }
                        var card = transaction.Card;
                        var cardCurrency = NormalizeCurrency(card.Currency);
                        var convertedAmountCard = await _currencyService.ConvertAsync(transaction.Amount, trCurrency, cardCurrency);
                        if (convertedAmountCard <= 0)
                        {
                            await databaseTransaction.RollbackAsync();
                            return Json(new { success = false, message = $"'{transaction.Description}' üçün valyuta çevrilməsi uğursuz oldu." });
                        }
                        if (transaction.IsIncome)
                        {
                            if (card.Balance < convertedAmountCard)
                            {
                                await databaseTransaction.RollbackAsync();
                                return Json(new { success = false, message = $"'{transaction.Description}' gəlirini silmək mümkün olmadı. Kart balansı kifayət etmir!" });
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
                    // Nağd Əməliyyat
                    else
                    {
                        if (transaction.IsIncome)
                        {
                            if (user.CashBalance < amountInAzn)
                            {
                                await databaseTransaction.RollbackAsync();
                                return Json(new { success = false, message = $"'{transaction.Description}' gəlirini silmək mümkün olmadı. Nağd balans kifayət etmir!" });
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
                }
                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();
                return Json(new { success = true, message = $"{transactions.Count} əməliyyat uğurla silindi." });
            }
            catch (Exception ex)
            {
                await databaseTransaction.RollbackAsync();
                _logger.LogError(ex, "BulkDelete əməliyyatı zamanı xəta baş verdi. İstifadəçi ID: {UserId}", user.Id);

                return Json(new { success = false, message = "Çoxlu silmə zamanı texniki xəta baş verdi." });
            }
        }
        // ==========================================
        // GET: /Transaction/Edit/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            // ------------------------------------------
            // 1. QONAQ İSTİFADƏÇİ (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                var guestTr = guestTransactions.FirstOrDefault(t => t.Id == id);

                if (guestTr == null)
                {
                    TempData["Error"] = "Əməliyyat tapılmadı.";
                    return RedirectToAction(nameof(Index));
                }
                var guestVm = new TransactionUpdateVM
                {
                    Id = guestTr.Id,
                    Amount = guestTr.Amount,
                    Currency = guestTr.Currency,
                    Description = guestTr.Description,
                    Date = guestTr.Date,
                    IsIncome = guestTr.IsIncome,
                    Status = guestTr.Status ?? "Tamamlandı",
                    CategoryId = guestTr.CategoryId,
                    CardId = guestTr.CardId
                };
                // await PopulateDropdownsAsync(); // Əgər Dropdown-lar istifadə olunursa
                return View(guestVm);
            }
            // ------------------------------------------
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id && !t.IsDeleted);
            if (transaction == null)
            {
                TempData["Error"] = "Əməliyyat tapılmadı və ya sizə aid deyil.";
                return RedirectToAction(nameof(Index));
            }
            var updateVm = new TransactionUpdateVM
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Description = transaction.Description,
                Date = transaction.Date,
                IsIncome = transaction.IsIncome,
                Status = transaction.Status ?? "Tamamlandı",
                CategoryId = transaction.CategoryId,
                CardId = transaction.CardId
            };
            // await PopulateDropdownsAsync(user.Id); // Əgər Dropdown-lar istifadə olunursa
            return View(updateVm);
        }
        // ==========================================
        // POST: /Transaction/Edit
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TransactionUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                // View-da SelectList/Dropdown-lar istifadə edilirsə, burada yenidən doldurun:
                // await PopulateDropdownsAsync();
                return View(model);
            }
            if (model.Amount <= 0)
            {
                TempData["Error"] = "Məbləğ 0-dan böyük olmalıdır.";
                return RedirectToReferrerOrIndex();
            }
            // CardId 0 və ya daha kiçik olduqda Nağd pul kimi qəbul edirik
            int? normalizedCardId = (model.CardId.HasValue && model.CardId.Value > 0) ? model.CardId.Value : null;
            var user = await _userManager.GetUserAsync(User);
            // ==========================================
            // 1. QONAQ İSTİFADƏÇİ (SESSION)
            // ==========================================
            if (user == null)
            {
                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                var guestTr = guestTransactions.FirstOrDefault(t => t.Id == model.Id);

                if (guestTr == null)
                {
                    TempData["Error"] = "Əməliyyat tapılmadı.";
                    return RedirectToAction(nameof(Index));
                }
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                var guestCashBalance = HttpContext.Session.GetObject<decimal?>("Guest_CashBalance") ?? 0m;
                // A. Köhnə əməliyyatın təsirini balansdan geri qaytarırıq (Revert)
                var oldCurrency = NormalizeCurrency(guestTr.Currency);
                if (guestTr.CardId.HasValue && guestTr.CardId.Value > 0)
                {
                    var oldCard = guestCards.FirstOrDefault(c => c.Id == guestTr.CardId.Value);
                    if (oldCard == null)
                    {
                        TempData["Error"] = "Əvvəlki əməliyyatın aid olduğu kart tapılmadı və ya silinib!";
                        return RedirectToReferrerOrIndex();
                    }
                    var oldCardCurrency = NormalizeCurrency(oldCard.Currency);
                    var oldCardAmount = await _currencyService.ConvertAsync(guestTr.Amount, oldCurrency, oldCardCurrency);
                    if (oldCardAmount <= 0)
                    {
                        TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                        return RedirectToReferrerOrIndex();
                    }
                    if (guestTr.IsIncome)
                    {
                        if (oldCard.Balance < oldCardAmount)
                        {
                            TempData["Error"] = "Köhnə gəlir əməliyyatını dəyişmək mümkün deyil. Kartda kifayət qədər vəsait yoxdur!";
                            return RedirectToReferrerOrIndex();
                        }
                        oldCard.Balance -= oldCardAmount;
                    }
                    else
                    {
                        oldCard.Balance += oldCardAmount;
                    }
                }
                else
                {
                    var oldAmountAzn = await _currencyService.ConvertAsync(guestTr.Amount, oldCurrency, "AZN");
                    if (oldAmountAzn <= 0)
                    {
                        TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                        return RedirectToReferrerOrIndex();
                    }

                    if (guestTr.IsIncome)
                    {
                        if (guestCashBalance < oldAmountAzn)
                        {
                            TempData["Error"] = "Köhnə gəlir əməliyyatını dəyişmək mümkün deyil. Nağd balansda kifayət qədər vəsait yoxdur!";
                            return RedirectToReferrerOrIndex();
                        }
                        guestCashBalance -= oldAmountAzn;
                    }
                    else
                    {
                        guestCashBalance += oldAmountAzn;
                    }
                }
                // B. Yeni parametrlərə əsasən balansı tətbiq edirik (Apply)
                var newCurrency = NormalizeCurrency(model.Currency);
                if (normalizedCardId.HasValue)
                {
                    var newGuestCard = guestCards.FirstOrDefault(c => c.Id == normalizedCardId.Value);
                    if (newGuestCard == null)
                    {
                        TempData["Error"] = "Seçilmiş yeni kart tapılmadı və ya silinib!";
                        return RedirectToReferrerOrIndex();
                    }
                    var newCardCurrency = NormalizeCurrency(newGuestCard.Currency);
                    var newCardAmount = await _currencyService.ConvertAsync(model.Amount, newCurrency, newCardCurrency);
                    if (newCardAmount <= 0)
                    {
                        TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                        return RedirectToReferrerOrIndex();
                    }
                    if (!model.IsIncome && newGuestCard.Balance < newCardAmount)
                    {
                        TempData["Error"] = $"Seçilmiş kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {newCardAmount:N2} {newCardCurrency})";
                        return RedirectToReferrerOrIndex();
                    }
                    if (model.IsIncome)
                        newGuestCard.Balance += newCardAmount;
                    else
                        newGuestCard.Balance -= newCardAmount;
                }
                else
                {
                    var newAmountAzn = await _currencyService.ConvertAsync(model.Amount, newCurrency, "AZN");
                    if (newAmountAzn <= 0)
                    {
                        TempData["Error"] = "Valyuta çevrilməsi uğursuz oldu.";
                        return RedirectToReferrerOrIndex();
                    }
                    if (!model.IsIncome && guestCashBalance < newAmountAzn)
                    {
                        TempData["Error"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Tələb olunan: {newAmountAzn:N2} AZN)";
                        return RedirectToReferrerOrIndex();
                    }
                    if (model.IsIncome)
                        guestCashBalance += newAmountAzn;
                    else
                        guestCashBalance -= newAmountAzn;
                }
                // C. Əməliyyat obyektinin sahələrini yeniləyirik
                guestTr.Amount = model.Amount;
                guestTr.Currency = newCurrency;
                guestTr.Description = model.Description;
                guestTr.Date = model.Date;
                guestTr.IsIncome = model.IsIncome;
                guestTr.CategoryId = model.CategoryId;
                guestTr.CardId = normalizedCardId;
                if (!string.IsNullOrEmpty(model.Status))
                {
                    guestTr.Status = model.Status;
                }
                // Session məlumatlarını saxlayırıq
                HttpContext.Session.SetObject("Guest_Cards", guestCards);
                HttpContext.Session.SetObject("Guest_CashBalance", guestCashBalance);
                HttpContext.Session.SetObject("Guest_Transactions", guestTransactions);
                TempData["SuccessMessage"] = "Əməliyyat və balanslar uğurla yeniləndi!";
                return RedirectToAction(nameof(Index));
            }
            // ==========================================
            // 2. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ==========================================
            var transaction = await _context.Transactions
                .Include(t => t.Card)
                .FirstOrDefaultAsync(t => t.Id == model.Id && t.UserId == user.Id && !t.IsDeleted);
            if (transaction == null)
            {
                TempData["Error"] = "Əməliyyat tapılmadı və ya sizə aid deyil.";
                return RedirectToAction(nameof(Index));
            }
            using var databaseTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ------------------------------------------
                // A. KÖHNƏ ƏMƏLİYYATIN TƏSİRİNİ BALANSDAN GERİ QAYTARIRIQ
                // ------------------------------------------
                var oldCurrency = NormalizeCurrency(transaction.Currency);
                var oldAmountAzn = await _currencyService.ConvertAsync(transaction.Amount, oldCurrency, "AZN");
                if (oldAmountAzn <= 0)
                {
                    await databaseTransaction.RollbackAsync();
                    TempData["Error"] = "Valyuta çevrilməsi zamanı xəta baş verdi.";
                    return RedirectToReferrerOrIndex();
                }
                if (transaction.CardId.HasValue)
                {
                    if (transaction.Card == null)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = "Əvvəlki əməliyyatın aid olduğu kart tapılmadı və ya silinib.";
                        return RedirectToReferrerOrIndex();
                    }
                    var cardCurrency = NormalizeCurrency(transaction.Card.Currency);
                    var oldCardAmount = await _currencyService.ConvertAsync(transaction.Amount, oldCurrency, cardCurrency);
                    if (oldCardAmount <= 0)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = "Valyuta çevrilməsi zamanı xəta baş verdi.";
                        return RedirectToReferrerOrIndex();
                    }
                    if (transaction.IsIncome)
                    {
                        if (transaction.Card.Balance < oldCardAmount)
                        {
                            await databaseTransaction.RollbackAsync();
                            TempData["Error"] = "Köhnə gəlir əməliyyatını dəyişmək mümkün deyil. Kart balansı kifayət etmir!";
                            return RedirectToReferrerOrIndex();
                        }
                        transaction.Card.Balance -= oldCardAmount;
                        user.TotalBalance -= oldAmountAzn;
                    }
                    else
                    {
                        transaction.Card.Balance += oldCardAmount;
                        user.TotalBalance += oldAmountAzn;
                    }
                }
                else
                {
                    if (transaction.IsIncome)
                    {
                        if (user.CashBalance < oldAmountAzn)
                        {
                            await databaseTransaction.RollbackAsync();
                            TempData["Error"] = "Köhnə gəlir əməliyyatını dəyişmək mümkün deyil. Nağd balans kifayət etmir!";
                            return RedirectToReferrerOrIndex();
                        }
                        user.CashBalance -= oldAmountAzn;
                        user.TotalBalance -= oldAmountAzn;
                    }
                    else
                    {
                        user.CashBalance += oldAmountAzn;
                        user.TotalBalance += oldAmountAzn;
                    }
                }
                // ------------------------------------------
                // B. YENİ MƏLUMATLARA ƏSASƏN BALANSI HESABLAYIB TƏTBİQ EDİRİK
                // ------------------------------------------
                var newCurrency = NormalizeCurrency(model.Currency);
                var newAmountAzn = await _currencyService.ConvertAsync(model.Amount, newCurrency, "AZN");
                if (newAmountAzn <= 0)
                {
                    await databaseTransaction.RollbackAsync();
                    TempData["Error"] = "Yeni valyuta çevrilməsi zamanı xəta baş verdi.";
                    return RedirectToReferrerOrIndex();
                }
                if (normalizedCardId.HasValue)
                {
                    // Orijinal kartla eynidirsə, bazaya təkrar sorğu atmamaq üçün mövcud obyekti istifadə edirik
                    Card? newCard = (transaction.Card != null && transaction.Card.Id == normalizedCardId.Value)
                        ? transaction.Card
                        : await _context.Cards.FirstOrDefaultAsync(c => c.Id == normalizedCardId.Value && c.UserId == user.Id && !c.IsDeleted);
                    if (newCard == null)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = "Seçilmiş yeni kart tapılmadı və ya silinib!";
                        return RedirectToReferrerOrIndex();
                    }
                    var newCardCurrency = NormalizeCurrency(newCard.Currency);
                    var newCardAmount = await _currencyService.ConvertAsync(model.Amount, newCurrency, newCardCurrency);
                    if (newCardAmount <= 0)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = "Kart valyutasının çevrilməsi zamanı xəta baş verdi.";
                        return RedirectToReferrerOrIndex();
                    }
                    if (!model.IsIncome && newCard.Balance < newCardAmount)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = $"Seçilmiş kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {newCardAmount:N2} {newCardCurrency})";
                        return RedirectToReferrerOrIndex();
                    }
                    if (model.IsIncome)
                    {
                        newCard.Balance += newCardAmount;
                        user.TotalBalance += newAmountAzn;
                    }
                    else
                    {
                        newCard.Balance -= newCardAmount;
                        user.TotalBalance -= newAmountAzn;
                    }
                }
                else
                {
                    if (!model.IsIncome && user.CashBalance < newAmountAzn)
                    {
                        await databaseTransaction.RollbackAsync();
                        TempData["Error"] = $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Tələb olunan: {newAmountAzn:N2} AZN, Mövcud: {user.CashBalance:N2} AZN)";
                        return RedirectToReferrerOrIndex();
                    }
                    if (model.IsIncome)
                    {
                        user.CashBalance += newAmountAzn;
                        user.TotalBalance += newAmountAzn;
                    }
                    else
                    {
                        user.CashBalance -= newAmountAzn;
                        user.TotalBalance -= newAmountAzn;
                    }
                }
                // ------------------------------------------
                // C. ƏMƏLİYYAT SAHƏLƏRİNİ YENİLƏYİRİK VƏ YAZIRIQ
                // ------------------------------------------
                transaction.Amount = model.Amount;
                transaction.Currency = newCurrency;
                transaction.Description = model.Description;
                transaction.Date = model.Date;
                transaction.IsIncome = model.IsIncome;
                transaction.CategoryId = model.CategoryId;
                transaction.CardId = normalizedCardId;
                if (!string.IsNullOrEmpty(model.Status))
                {
                    transaction.Status = model.Status;
                }
                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();
                TempData["SuccessMessage"] = "Əməliyyat və balanslar uğurla yeniləndi!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await databaseTransaction.RollbackAsync();
                _logger.LogError(ex, "Transaction Edit əməliyyatı zamanı xəta baş verdi. Transaction ID: {TransactionId}, İstifadəçi ID: {UserId}", model.Id, user.Id);

                TempData["Error"] = "Əməliyyat yenilənərkən texniki xəta baş verdi.";
                return RedirectToReferrerOrIndex();
            }
        }

        // ==========================================
        // 1. SÜNİ İNTELLEKT İLƏ QƏBZ OXUTMA
        // ==========================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ProcessReceiptImage(IFormFile receiptImage)
        {
            if (receiptImage == null || receiptImage.Length == 0)
            {
                return Json(new { success = false, message = "Şəkil faylı göndərilməyib və ya boşdur!" });
            }

            try
            {
                // API Key appsettings.json faylından oxunur
                string apiKey = _configuration["Gemini:ApiKey"] ?? "";
                string geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

                using var ms = new MemoryStream();
                await receiptImage.CopyToAsync(ms);
                string base64Image = Convert.ToBase64String(ms.ToArray());
                string mimeType = receiptImage.ContentType ?? "image/jpeg";

                string promptText = @"
You are an OCR engine specialized for shopping receipts.

Read the receipt carefully.

Return ONLY valid JSON.

Do not use markdown.

Do not explain anything.

JSON format:

{
  ""amount"": 0,
  ""store"": """",
  ""date"": """",
  ""description"": """"
}

Rules:

- amount = FINAL PAID AMOUNT.
- Never use item price.
- Never use discount.
- Never use VAT.

- store = company/store name.

- date = receipt date in yyyy-MM-dd.

- description = write ALL purchased products separated by commas.

Example:

{
  ""amount"":24.91,
  ""store"":""QAYALI GOLD MARKET"",
  ""date"":""2026-08-05"",
  ""description"":""Morfose Cream, Oxidant 9%, Shampoo""
}

If a value cannot be found return null.
";

                var requestBody = new
                {
                    contents = new object[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = promptText },
                                new
                                {
                                    inline_data = new { mime_type = mimeType, data = base64Image }
                                }
                            }
                        }
                    }
                };

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(geminiApiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errStr = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API Error: {errStr}");
                    return Json(new { success = false, message = $"Google API xətası ({response.StatusCode}): {errStr}" });
                }

                string responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Gemini Xam Cavabı: {responseString}");

                using var doc = JsonDocument.Parse(responseString);

                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return Json(new { success = false, message = "Gemini cavab qaytarmadı və ya şəkil bloklandı." });
                }

                string aiRawText = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";

                _logger.LogInformation($"AI Çıxarılan Mətn: {aiRawText}");

                aiRawText = aiRawText.Trim();
                int start = aiRawText.IndexOf('{');
                int end = aiRawText.LastIndexOf('}');

                if (start >= 0 && end > start)
                {
                    aiRawText = aiRawText.Substring(start, end - start + 1);
                }

                using var resultData = JsonDocument.Parse(aiRawText);
                var root = resultData.RootElement;

                string amountStr = root.TryGetProperty("amount", out var a) ? a.ToString() : "0";
                string store = root.TryGetProperty("store", out var s) ? s.GetString() ?? "Qəbz Əməliyyatı" : "Qəbz Əməliyyatı";
                string dateStr = root.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "";
                string description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? store : store;

                amountStr = amountStr
                    .Replace("AZN", "")
                    .Replace("₼", "")
                    .Replace(" ", "")
                    .Replace(",", ".");

                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedAmount))
                {
                    parsedAmount = 0;
                }

                DateTime parsedDate = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(dateStr))
                {
                    if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tempDate))
                    {
                        parsedDate = tempDate;
                    }
                    else if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDate))
                    {
                        parsedDate = tempDate;
                    }
                }

                if (parsedDate.Year < 2020)
                {
                    parsedDate = DateTime.Today;
                }

                return Json(new
                {
                    success = true,
                    amount = parsedAmount,
                    store = store,
                    description = description,
                    date = parsedDate.ToString("yyyy-MM-dd"),
                    message = "Qəbz uğurla oxundu."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Qəbz oxuma xətası");
                return Json(new { success = false, message = "Sistem xətası: " + ex.Message });
            }
        }
        // ==========================================
        // GET: /Transaction/Create
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(string? amount, string? store, string? description, string? date, string? fiscalId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Qonaq və ya qeydiyyatlı istifadəçi üçün dropdown-ları doldururuq
            await PopulateCategoryAndCardDropDownsAsync(user);

            var model = new TransactionCreateVM
            {
                Date = DateTime.Now
            };

            // 1. QR / AI Oxumadan gələn Məbləğ (Amount) məlumatı
            if (!string.IsNullOrWhiteSpace(amount))
            {
                string rawAmount = amount.Replace(',', '.');
                if (decimal.TryParse(rawAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedAmount))
                {
                    model.Amount = parsedAmount;
                }
            }

            // 2. Açıqlama və ya Mağaza adı (Description / Store)
            model.Description = !string.IsNullOrWhiteSpace(description)
                ? description
                : store;

            // 3. Tarix (Date)
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out DateTime parsedDate))
            {
                model.Date = parsedDate;
            }

            // Əgər Fiskal ID gəlibsə, onu da açıqlamaya və ya müvafiq xanaya əlavə edə bilərsiniz
            if (!string.IsNullOrWhiteSpace(fiscalId) && string.IsNullOrWhiteSpace(model.Description))
            {
                model.Description = $"Fiskal ID: {fiscalId}";
            }

            return View(model);
        }

        // ==========================================
        // POST: /Transaction/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionCreateVM model)
        {
            var user = await _userManager.GetUserAsync(User);

            // 🟢 0. QƏPİK / VERGÜL XƏTASINI KÖKÜNDƏN HƏLL EDƏN HİSSƏ
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

            // 1. Model Validasiyası Yoxlanışı
            if (!ModelState.IsValid)
            {
                await PopulateCategoryAndCardDropDownsAsync(user);
                return View(model);
            }

            // 2. Məbləğ Yoxlanışı
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Məbləğ 0-dan böyük olmalıdır.");
                await PopulateCategoryAndCardDropDownsAsync(user);
                return View(model);
            }

            var transactionCurrency = NormalizeCurrency(model.Currency ?? "AZN");

            // ------------------------------------------
            // A. QONAQ İSTİFADƏÇİ LOGİKASI (SESSION)
            // ------------------------------------------
            if (user == null)
            {
                var guestCards = HttpContext.Session.GetObject<List<Card>>("Guest_Cards") ?? new List<Card>();
                var guestCategories = HttpContext.Session.GetObject<List<Category>>("Guest_Categories") ?? new List<Category>();
                Card? selectedCard = null;

                if (model.CardId.HasValue && model.CardId.Value > 0)
                {
                    selectedCard = guestCards.FirstOrDefault(c => c.Id == model.CardId.Value);
                    if (selectedCard == null)
                    {
                        ModelState.AddModelError("", "Seçilmiş kart tapılmadı və ya silinib!");
                        await PopulateCategoryAndCardDropDownsAsync(null);
                        return View(model);
                    }
                }

                if (selectedCard != null)
                {
                    var cardCurrency = NormalizeCurrency(selectedCard.Currency);
                    var convertedAmount = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, cardCurrency);
                    if (convertedAmount <= 0)
                    {
                        ModelState.AddModelError("", "Valyuta çevrilməsi uğursuz oldu.");
                        await PopulateCategoryAndCardDropDownsAsync(null);
                        return View(model);
                    }

                    if (!model.IsIncome && selectedCard.Balance < convertedAmount)
                    {
                        ModelState.AddModelError("", $"Kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {convertedAmount:N2} {cardCurrency})");
                        await PopulateCategoryAndCardDropDownsAsync(null);
                        return View(model);
                    }

                    if (model.IsIncome)
                        selectedCard.Balance += convertedAmount;
                    else
                        selectedCard.Balance -= convertedAmount;

                    HttpContext.Session.SetObject("Guest_Cards", guestCards);
                }
                else
                {
                    var guestCashBalance = HttpContext.Session.GetObject<decimal?>("Guest_CashBalance") ?? 0m;
                    var convertedAmountAzn = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, "AZN");
                    if (convertedAmountAzn <= 0)
                    {
                        ModelState.AddModelError("", "Valyuta çevrilməsi uğursuz oldu.");
                        await PopulateCategoryAndCardDropDownsAsync(null);
                        return View(model);
                    }

                    if (!model.IsIncome && guestCashBalance < convertedAmountAzn)
                    {
                        ModelState.AddModelError("", $"Nağd balansda kifayət qədər vəsait yoxdur! (Tələb olunan: {convertedAmountAzn:N2} AZN)");
                        await PopulateCategoryAndCardDropDownsAsync(null);
                        return View(model);
                    }

                    if (model.IsIncome)
                        guestCashBalance += convertedAmountAzn;
                    else
                        guestCashBalance -= convertedAmountAzn;

                    HttpContext.Session.SetObject("Guest_CashBalance", guestCashBalance);
                }

                string guestCategoryName = "Ümumi";
                if (!string.IsNullOrWhiteSpace(model.NewCategoryName))
                {
                    guestCategoryName = model.NewCategoryName.Trim();
                }
                else if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
                {
                    var selectedGuestCategory = guestCategories.FirstOrDefault(c => c.Id == model.CategoryId.Value);
                    if (selectedGuestCategory != null)
                    {
                        guestCategoryName = selectedGuestCategory.Name;
                    }
                }

                var guestTransactions = HttpContext.Session.GetObject<List<Transaction>>("Guest_Transactions") ?? new List<Transaction>();
                var guestTransaction = new Transaction
                {
                    Id = guestTransactions.Any() ? guestTransactions.Max(t => t.Id) + 1 : 1,
                    CardId = selectedCard?.Id,
                    Amount = model.Amount,
                    Description = model.Description,
                    IsIncome = model.IsIncome,
                    Date = model.Date == default ? DateTime.Now : model.Date,
                    Currency = transactionCurrency,
                    Status = string.IsNullOrWhiteSpace(model.Status) ? "Tamamlandı" : model.Status,
                    Category = new Category { Name = guestCategoryName }
                };

                guestTransactions.Add(guestTransaction);
                HttpContext.Session.SetObject("Guest_Transactions", guestTransactions);
                TempData["SuccessMessage"] = "Əməliyyat sınaq rejimində (Session) əlavə olundu!";

                string refererGuest = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(refererGuest) && !refererGuest.Contains("/Transaction/Create"))
                {
                    return Redirect(refererGuest);
                }

                return RedirectToAction("Index");
            }

            // ------------------------------------------
            // B. QEYDİYYATLI İSTİFADƏÇİ (DATABASE)
            // ------------------------------------------
            Card? card = null;
            decimal convertedAmountSql = 0;
            decimal amountInAzn = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, "AZN");
            if (amountInAzn <= 0)
            {
                ModelState.AddModelError("", "AZN ekvivalentinin hesablanması zamanı xəta baş verdi.");
                await PopulateCategoryAndCardDropDownsAsync(user);
                return View(model);
            }

            if (model.CardId.HasValue && model.CardId.Value > 0)
            {
                card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == model.CardId && c.UserId == user.Id && !c.IsDeleted);
                if (card == null)
                {
                    ModelState.AddModelError("", "Seçilmiş kart tapılmadı və ya silinib!");
                    await PopulateCategoryAndCardDropDownsAsync(user);
                    return View(model);
                }

                var cardCurrency = NormalizeCurrency(card.Currency);
                convertedAmountSql = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, cardCurrency);
                if (convertedAmountSql <= 0)
                {
                    ModelState.AddModelError("", "Valyuta çevrilməsi uğursuz oldu.");
                    await PopulateCategoryAndCardDropDownsAsync(user);
                    return View(model);
                }

                if (!model.IsIncome && card.Balance < convertedAmountSql)
                {
                    ModelState.AddModelError("", $"Kartda kifayət qədər vəsait yoxdur! (Tələb olunan: {convertedAmountSql:N2} {cardCurrency})");
                    await PopulateCategoryAndCardDropDownsAsync(user);
                    return View(model);
                }
            }
            else
            {
                if (!model.IsIncome && user.CashBalance < amountInAzn)
                {
                    ModelState.AddModelError("", $"Nağd balansınızda kifayət qədər vəsait yoxdur! (Tələb olunan: {amountInAzn:N2} AZN)");
                    await PopulateCategoryAndCardDropDownsAsync(user);
                    return View(model);
                }
            }

            using var databaseTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string finalCategoryName = string.Empty;

                if (!string.IsNullOrWhiteSpace(model.NewCategoryName))
                {
                    finalCategoryName = model.NewCategoryName.Trim();
                }
                else if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
                {
                    var catDb = await _context.Categories.FirstOrDefaultAsync(c =>
                        c.Id == model.CategoryId.Value &&
                        (c.UserId == user.Id || c.UserId == null));

                    if (catDb != null) finalCategoryName = catDb.Name;
                }

                if (string.IsNullOrWhiteSpace(finalCategoryName))
                    finalCategoryName = "Ümumi";

                var category = await _categoryService.GetOrCreateAsync(finalCategoryName, model.IsIncome, user.Id);
                if (category == null)
                {
                    await databaseTransaction.RollbackAsync();
                    ModelState.AddModelError("", "Kateqoriya yaradılarkən xəta baş verdi.");
                    await PopulateCategoryAndCardDropDownsAsync(user);
                    return View(model);
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

                await _context.Transactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();

                TempData["SuccessMessage"] = "Əməliyyat uğurla qeydə alındı!";

                string refererAuth = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(refererAuth) && !refererAuth.Contains("/Transaction/Create"))
                {
                    return Redirect(refererAuth);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await databaseTransaction.RollbackAsync();
                _logger.LogError(ex, "Transaction Create əməliyyatı zamanı xəta baş verdi.");

                TempData["Error"] = "Xəta: " + (ex.InnerException?.Message ?? ex.Message);
                await PopulateCategoryAndCardDropDownsAsync(user);
                return View(model);
            }
        }

        // ==========================================
        // AJAX İLƏ KATEQORİYA YARATMA
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CreateCategoryAjax([FromBody] QuickCategoryRequest model)
        {
            if (string.IsNullOrWhiteSpace(model?.Name))
            {
                return Json(new { success = false, message = "Kateqoriya adı boş ola bilməz!" });
            }

            return Json(new { success = true, name = model.Name.Trim() });
        }

        public class QuickCategoryRequest
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}