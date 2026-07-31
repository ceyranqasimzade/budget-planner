using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.ViewModels;

namespace budget_planner.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly BudgetDbContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly IWebHostEnvironment _env;

        public TransactionService(
            BudgetDbContext context,
            ICurrencyService currencyService,
            IWebHostEnvironment env)
        {
            _context = context;
            _currencyService = currencyService;
            _env = env;
        }

        // =========================================================
        // 1. AUTHENTICATED USER CRUD
        // =========================================================

        public async Task<bool> CreateTransactionAsync(string userId, TransactionCreateVM model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            string transactionCurrency = model.Currency ?? "AZN";
            decimal amountInAzn = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, "AZN");

            decimal cardConvertedAmount = model.Amount;
            Card card = null;

            if (model.CardId.HasValue)
            {
                card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == model.CardId.Value && c.UserId == userId);
                if (card == null) throw new Exception("Seçilmiş kart tapılmadı!");

                cardConvertedAmount = await _currencyService.ConvertAsync(model.Amount, transactionCurrency, card.Currency);

                if (!model.IsIncome && card.Balance < cardConvertedAmount)
                {
                    throw new Exception($"Kartda xərc üçün kifayət qədər vəsait yoxdur! (Balans: {card.Balance:N2} {card.Currency})");
                }
            }
            else
            {
                if (!model.IsIncome && user.CashBalance < amountInAzn)
                {
                    throw new Exception($"Nağd balansda kifayət qədər vəsait yoxdur! (Balans: {user.CashBalance:N2} AZN)");
                }
            }

            string receiptUrl = await SaveReceiptFileAsync(model.ReceiptFile);

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int categoryId = await GetOrCreateCategoryIdAsync(model.CategoryId, model.NewCategoryName, userId);

                var transaction = new Transaction
                {
                    UserId = userId,
                    Amount = model.Amount,
                    Currency = transactionCurrency,
                    Description = model.Description,
                    Date = model.Date,
                    IsIncome = model.IsIncome,
                    Status = string.IsNullOrWhiteSpace(model.Status) ? "Tamamlandı" : model.Status,
                    CategoryId = categoryId,
                    CardId = model.CardId,
                    ReceiptUrl = receiptUrl,
                    IsRecurring = model.IsRecurring,
                    RecurringFrequency = model.IsRecurring ? model.RecurringFrequency : null
                };

                _context.Transactions.Add(transaction);

                if (card != null)
                {
                    card.Balance += model.IsIncome ? cardConvertedAmount : -cardConvertedAmount;
                    _context.Cards.Update(card);
                }
                else
                {
                    user.CashBalance += model.IsIncome ? amountInAzn : -amountInAzn;
                }

                user.TotalBalance += model.IsIncome ? amountInAzn : -amountInAzn;
                _context.Users.Update(user);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                DeleteReceiptFile(receiptUrl);
                throw;
            }
        }

        public async Task<bool> UpdateTransactionAsync(string userId, TransactionUpdateVM model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var t = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == model.Id && x.UserId == userId && !x.IsDeleted);
            if (user == null || t == null) return false;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await RevertTransactionBalanceAsync(user, t);

                if (model.NewReceiptFile != null && model.NewReceiptFile.Length > 0)
                {
                    t.ReceiptUrl = await SaveReceiptFileAsync(model.NewReceiptFile, t.ReceiptUrl);
                }

                t.Amount = model.Amount;
                t.Currency = model.Currency ?? "AZN";
                t.Description = model.Description;
                t.Date = model.Date;
                t.IsIncome = model.IsIncome;
                t.Status = model.Status;
                t.CardId = model.CardId;
                t.IsRecurring = model.IsRecurring;
                t.RecurringFrequency = model.IsRecurring ? model.RecurringFrequency : null;

                // Variant 2: NewCategoryName yoxdur, yalnız siyahıdan ID seçilir
                if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
                {
                    t.CategoryId = model.CategoryId.Value;
                }

                await ApplyNewTransactionBalanceAsync(user, t);

                _context.Transactions.Update(t);
                _context.Users.Update(user);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteTransactionAsync(string userId, int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var t = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);
            if (user == null || t == null) return false;

            decimal amountInAzn = await _currencyService.ConvertAsync(t.Amount, t.Currency, "AZN");

            if (t.IsIncome)
            {
                if (t.CardId.HasValue)
                {
                    var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == t.CardId.Value && c.UserId == userId);
                    if (card != null)
                    {
                        decimal cardConverted = await _currencyService.ConvertAsync(t.Amount, t.Currency, card.Currency);
                        if (card.Balance < cardConverted)
                            throw new Exception("Bu gəlir silinsə, kartın balansı mənfiyə düşəcək!");
                    }
                }
                else if (user.CashBalance < amountInAzn)
                {
                    throw new Exception("Bu gəlir silinsə, nağd balansınız mənfiyə düşəcək!");
                }
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await RevertTransactionBalanceAsync(user, t);

                t.IsDeleted = true;
                DeleteReceiptFile(t.ReceiptUrl);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        // =========================================================
        // 2. GUEST SESSION CRUD
        // =========================================================

        public async Task CreateGuestTransactionAsync(ISession session, TransactionCreateVM model, string receiptUrl = null)
        {
            decimal currentBalance = GetGuestCashBalance(session);
            decimal amountInAzn = await _currencyService.ConvertAsync(model.Amount, model.Currency ?? "AZN", "AZN");

            if (!model.IsIncome && currentBalance < amountInAzn)
            {
                throw new Exception($"Nağd balansda kifayət qədər vəsait yoxdur! (Qonaq balansı: {currentBalance:N2} AZN)");
            }

            if (string.IsNullOrEmpty(receiptUrl) && model.ReceiptFile != null && model.ReceiptFile.Length > 0)
            {
                receiptUrl = await SaveReceiptFileAsync(model.ReceiptFile);
            }

            var list = GetGuestTransactions(session);
            int newId = list.Any() ? list.Max(x => x.Id) + 1 : 1;

            list.Insert(0, new TransactionVM
            {
                Id = newId,
                Amount = model.Amount,
                Currency = model.Currency ?? "AZN",
                Description = model.Description,
                Date = model.Date,
                IsIncome = model.IsIncome,
                Status = string.IsNullOrWhiteSpace(model.Status) ? "Tamamlandı" : model.Status,
                CategoryName = !string.IsNullOrWhiteSpace(model.NewCategoryName) ? model.NewCategoryName : "Ümumi",
                CardName = "Nağd",
                ReceiptUrl = receiptUrl,
                IsRecurring = model.IsRecurring,
                RecurringFrequency = model.IsRecurring ? model.RecurringFrequency : null
            });

            currentBalance += model.IsIncome ? amountInAzn : -amountInAzn;
            SaveGuestSessionData(session, list, currentBalance);
        }

        public async Task UpdateGuestTransactionAsync(ISession session, TransactionUpdateVM model)
        {
            var list = GetGuestTransactions(session);
            var item = list.FirstOrDefault(x => x.Id == model.Id);
            if (item == null) return;

            decimal currentBalance = GetGuestCashBalance(session);

            decimal oldAmountInAzn = await _currencyService.ConvertAsync(item.Amount, item.Currency ?? "AZN", "AZN");
            currentBalance += item.IsIncome ? -oldAmountInAzn : oldAmountInAzn;

            decimal newAmountInAzn = await _currencyService.ConvertAsync(model.Amount, model.Currency ?? "AZN", "AZN");
            if (!model.IsIncome && currentBalance < newAmountInAzn)
            {
                throw new Exception("Yeni məbləğ üçün balansda vəsait çatmır!");
            }

            if (model.NewReceiptFile != null && model.NewReceiptFile.Length > 0)
            {
                item.ReceiptUrl = await SaveReceiptFileAsync(model.NewReceiptFile, item.ReceiptUrl);
            }

            item.Amount = model.Amount;
            item.Currency = model.Currency ?? "AZN";
            item.Description = model.Description;
            item.Date = model.Date;
            item.IsIncome = model.IsIncome;
            item.Status = model.Status;
            item.IsRecurring = model.IsRecurring;
            item.RecurringFrequency = model.IsRecurring ? model.RecurringFrequency : null;

            currentBalance += model.IsIncome ? newAmountInAzn : -newAmountInAzn;
            SaveGuestSessionData(session, list, currentBalance);
        }

        public async Task DeleteGuestTransactionAsync(ISession session, int id)
        {
            var list = GetGuestTransactions(session);
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item == null) return;

            decimal currentBalance = GetGuestCashBalance(session);
            decimal amountInAzn = await _currencyService.ConvertAsync(item.Amount, item.Currency ?? "AZN", "AZN");

            if (item.IsIncome && currentBalance < amountInAzn)
            {
                throw new Exception("Bu gəlir silinsə, nağd qonaq balansı mənfiyə düşəcək!");
            }

            currentBalance += item.IsIncome ? -amountInAzn : amountInAzn;
            DeleteReceiptFile(item.ReceiptUrl);
            list.Remove(item);

            SaveGuestSessionData(session, list, currentBalance);
        }

        // =========================================================
        // 3. FAYL TƏHLÜKƏSİZLİYİ & KÖMƏKÇİLƏR
        // =========================================================

        public async Task<string> SaveReceiptFileAsync(IFormFile file, string oldFilePath = null)
        {
            if (file == null || file.Length == 0) return oldFilePath;

            if (file.Length > 5 * 1024 * 1024)
            {
                throw new Exception("Qəbz faylının həcmi maksimum 5 MB ola bilər!");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "application/pdf" };

            var ext = Path.GetExtension(file.FileName).ToLower();
            var mimeType = file.ContentType.ToLower();

            if (!allowedExtensions.Contains(ext) || !allowedMimeTypes.Contains(mimeType))
            {
                throw new Exception("Fayl növü düzgün deyil. Yalnız JPG, PNG və ya PDF yükləyə bilərsiniz.");
            }

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            if (!string.IsNullOrEmpty(oldFilePath))
            {
                DeleteReceiptFile(oldFilePath);
            }

            return $"/uploads/receipts/{uniqueFileName}";
        }

        public void DeleteReceiptFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;
            try
            {
                string relativePath = fileUrl.TrimStart('/');
                string fullPath = Path.Combine(_env.WebRootPath, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch { }
        }

        public async Task<int> GetOrCreateCategoryIdAsync(int? categoryId, string newCategoryName, string userId)
        {
            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                string targetName = newCategoryName.Trim().ToUpper();

                var existing = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.Trim().ToUpper() == targetName && (c.UserId == userId || c.UserId == null));

                if (existing != null) return existing.Id;

                var newCat = new Category { Name = newCategoryName.Trim(), UserId = userId };
                _context.Categories.Add(newCat);
                await _context.SaveChangesAsync();
                return newCat.Id;
            }

            if (categoryId.HasValue && categoryId.Value > 0) return categoryId.Value;

            var defaultCat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Ümumi");
            return defaultCat?.Id ?? 1;
        }

        // =========================================================
        // 4. MALİYYƏ KÖMƏKÇİLƏRİ (PRIVATE HELPERS)
        // =========================================================

        private async Task RevertTransactionBalanceAsync(ApplicationUser user, Transaction t)
        {
            decimal amountInAzn = await _currencyService.ConvertAsync(t.Amount, t.Currency, "AZN");

            if (t.CardId.HasValue)
            {
                var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == t.CardId.Value);
                if (card != null)
                {
                    decimal cardConverted = await _currencyService.ConvertAsync(t.Amount, t.Currency, card.Currency);
                    card.Balance += t.IsIncome ? -cardConverted : cardConverted;
                    _context.Cards.Update(card);
                }
            }
            else
            {
                user.CashBalance += t.IsIncome ? -amountInAzn : amountInAzn;
            }

            user.TotalBalance += t.IsIncome ? -amountInAzn : amountInAzn;
        }

        private async Task ApplyNewTransactionBalanceAsync(ApplicationUser user, Transaction t)
        {
            decimal amountInAzn = await _currencyService.ConvertAsync(t.Amount, t.Currency, "AZN");

            if (t.CardId.HasValue)
            {
                var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == t.CardId.Value);
                if (card == null) throw new Exception("Seçilmiş kart tapılmadı!");

                decimal cardConverted = await _currencyService.ConvertAsync(t.Amount, t.Currency, card.Currency);

                if (!t.IsIncome && card.Balance < cardConverted)
                {
                    throw new Exception($"Seçilmiş kartda kifayət qədər vəsait yoxdur! (Cari balans: {card.Balance:N2} {card.Currency})");
                }

                card.Balance += t.IsIncome ? cardConverted : -cardConverted;
                _context.Cards.Update(card);
            }
            else
            {
                if (!t.IsIncome && user.CashBalance < amountInAzn)
                {
                    throw new Exception($"Nağd balansda kifayət qədər vəsait yoxdur! (Cari balans: {user.CashBalance:N2} AZN)");
                }

                user.CashBalance += t.IsIncome ? amountInAzn : -amountInAzn;
            }

            user.TotalBalance += t.IsIncome ? amountInAzn : -amountInAzn;
        }

        private List<TransactionVM> GetGuestTransactions(ISession session)
        {
            var json = session.GetString("Guest_Transactions");
            if (string.IsNullOrEmpty(json)) return new List<TransactionVM>();
            return JsonSerializer.Deserialize<List<TransactionVM>>(json) ?? new List<TransactionVM>();
        }

        private decimal GetGuestCashBalance(ISession session)
        {
            var str = session.GetString("Guest_CashBalance");
            return decimal.TryParse(str, out decimal bal) ? bal : 0m;
        }

        private void SaveGuestSessionData(ISession session, List<TransactionVM> list, decimal newBalance)
        {
            session.SetString("Guest_Transactions", JsonSerializer.Serialize(list));
            session.SetString("Guest_CashBalance", newBalance.ToString("0.00"));
        }
    }
}