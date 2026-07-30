using budget_planner.Models;
using System.Threading.Tasks;

namespace budget_planner.Services
{
    public interface ICategoryService
    {
        Task<Category> GetOrCreateAsync(string? categoryName, bool isIncome, string userId);
    }
}