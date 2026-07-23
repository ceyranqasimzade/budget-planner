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


        public string? FamilyGroupId { get; set; } // Ailə üzvlərini bağlamaq üçün

        public ICollection<Transaction> Transactions { get; set; }
            = new List<Transaction>();


        public ICollection<Card> Cards { get; set; }
            = new List<Card>();


        public ICollection<Goal> Goals { get; set; }
            = new List<Goal>();


    }
    }