using budget_planner.DAL;
using budget_planner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
// Diqqət: SixLabors using-ni çıxardıq ki, EF Core ilə toqquşmasın

namespace budget_planner.Services
{
    // --- AVATAR SERVİSİ ---
    public interface IAvatarService
    {
        Task<bool> IsValidImageAsync(IFormFile file);
        Task<string> UploadAvatarAsync(IFormFile file, string? currentAvatarPath);
        void DeleteAvatar(string? avatarPath);
    }

    public class AvatarService : IAvatarService
    {
        private readonly IWebHostEnvironment _env;

        public AvatarService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<bool> IsValidImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024)
                return false;

            var safeFileName = Path.GetFileName(file.FileName) ?? string.Empty;
            var ext = Path.GetExtension(safeFileName)?.ToLowerInvariant() ?? string.Empty;

            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };

            // ContentType null gəlmə ehtimalına qarşı ?? "" istifadə edirik
            if (!allowedExts.Contains(ext) || !allowedContentTypes.Contains(file.ContentType ?? ""))
                return false;

            try
            {
                await using var stream = file.OpenReadStream();
                // Tam adla çağırırıq ki, EF Core ilə toqquşmasın!
                using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> UploadAvatarAsync(IFormFile file, string? currentAvatarPath)
        {
            if (!await IsValidImageAsync(file))
                throw new InvalidDataException("Fayl etibarlı şəkil deyil.");

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var safeFileName = Path.GetFileName(file.FileName) ?? string.Empty;
            var ext = Path.GetExtension(safeFileName)?.ToLowerInvariant() ?? string.Empty;
            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            DeleteAvatar(currentAvatarPath);

            return "/uploads/avatars/" + uniqueFileName;
        }

        public void DeleteAvatar(string? avatarPath)
        {
            if (!string.IsNullOrEmpty(avatarPath) && avatarPath.StartsWith("/uploads/avatars/"))
            {
                string fullPath = Path.Combine(_env.WebRootPath, avatarPath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
        }
    }

    // --- MƏLUMAT SİLMƏ SERVİSİ ---
    public interface IUserDataCleanupService
    {
        Task DeleteFinancialDataAsync(string userId);
        Task<IdentityResult> DeleteUserWithDataTransactionAsync(ApplicationUser user, UserManager<ApplicationUser> userManager);
    }

    public class UserDataCleanupService : IUserDataCleanupService
    {
        private readonly BudgetDbContext _context;

        public UserDataCleanupService(BudgetDbContext context)
        {
            _context = context;
        }

        public async Task DeleteFinancialDataAsync(string userId)
        {
            await _context.Transactions.Where(t => t.UserId == userId).ExecuteDeleteAsync();
            await _context.Cards.Where(c => c.UserId == userId).ExecuteDeleteAsync();
            await _context.Goals.Where(g => g.UserId == userId).ExecuteDeleteAsync();
            await _context.Categories.Where(c => c.UserId == userId).ExecuteDeleteAsync();
            await _context.UpcomingPayments.Where(u => u.UserId == userId).ExecuteDeleteAsync();
            // QEYD: Notifications cədvəli olmadığı üçün silindi.
        }

        public async Task<IdentityResult> DeleteUserWithDataTransactionAsync(ApplicationUser user, UserManager<ApplicationUser> userManager)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await DeleteFinancialDataAsync(user.Id);
                var result = await userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}