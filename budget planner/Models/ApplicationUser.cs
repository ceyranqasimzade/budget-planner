using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Ad və soyad daxil edilməlidir")]
        [StringLength(100, ErrorMessage = "Ad maksimum 100 simvol ola bilər")]
        public string FullName { get; set; } = null!;
        public BudgetRule? BudgetRule { get; set; }

        // Kontrollerin işləməsi üçün əlavə edilən xassələr
        public decimal TotalBalance { get; set; }
        public decimal CashBalance { get; set; }

        public ICollection<Transaction> Transactions { get; set; }
            = new List<Transaction>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public ICollection<Card> Cards { get; set; }
            = new List<Card>();

        public ICollection<Goal> Goals { get; set; }
            = new List<Goal>();
    }
}