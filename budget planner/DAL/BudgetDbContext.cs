using Microsoft.EntityFrameworkCore;
using budget_planner.Models;
namespace budget_planner.DAL
{
    public class BudgetDbContext : DbContext
    {
        public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }
    }
}