using budget_planner.DAL;
using budget_planner.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace budget_planner.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly BudgetDbContext _context;

        public CategoryService(BudgetDbContext context)
        {
            _context = context;
        }

        public async Task<Category> GetOrCreateAsync(string? categoryName, bool isIncome, string userId)
        {
            string categoryType = isIncome ? "Gəlir" : "Xərc";
            string name = string.IsNullOrWhiteSpace(categoryName)
                ? "Ümumi"
                : categoryName.Trim();

            // Unikal indeksə söykənərək SingleOrDefaultAsync istifadə edirik.
            // AsNoTracking istifadə EDİLMİR ki, EF Core obyekti izləyə bilsin.
            var category = await _context.Categories
                .SingleOrDefaultAsync(c => c.Name == name &&
                                           c.Type == categoryType &&
                                           c.UserId == userId);

            if (category != null)
            {
                return category;
            }

            category = new Category
            {
                Name = name,
                Type = categoryType,
                UserId = userId
            };

            _context.Categories.Add(category);
            return category;
        }
    }
}