using Microsoft.AspNetCore.Http;
using budget_planner.Models;
using budget_planner.ViewModels;

namespace budget_planner.Services
{
    public interface ITransactionService
    {
        // Registered User CRUD
        Task<bool> CreateTransactionAsync(string userId, TransactionCreateVM model);
        Task<bool> UpdateTransactionAsync(string userId, TransactionUpdateVM model);
        Task<bool> DeleteTransactionAsync(string userId, int id);

        // Guest User CRUD
        Task CreateGuestTransactionAsync(ISession session, TransactionCreateVM model, string receiptUrl = null);
        Task UpdateGuestTransactionAsync(ISession session, TransactionUpdateVM model);
        Task DeleteGuestTransactionAsync(ISession session, int id);

        // Files & Categories
        Task<string> SaveReceiptFileAsync(IFormFile file, string oldFilePath = null);
        void DeleteReceiptFile(string fileUrl);
        Task<int> GetOrCreateCategoryIdAsync(int? categoryId, string newCategoryName, string userId);
    }
}