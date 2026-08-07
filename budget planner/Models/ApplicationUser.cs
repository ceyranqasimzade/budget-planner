using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Ad və soyad daxil edilməlidir")]
        [StringLength(100, ErrorMessage = "Ad maksimum 100 simvol ola bilər")]
        public string FullName { get; set; } = string.Empty;

        public BudgetRule? BudgetRule { get; set; }

        // Kontrollerin işləməsi üçün əlavə edilən xassələr
        public decimal TotalBalance { get; set; }
        public decimal CashBalance { get; set; }

        // ⚙️ Parametrlər (Settings) bölməsi üçün əlavə edilən xassələr
        public string DefaultCurrency { get; set; } = "AZN";
        public string Theme { get; set; } = "dark";
        public string Language { get; set; } = "AZ";
        public bool BudgetAlerts { get; set; } = true;
        public bool EmailNotifications { get; set; } = true;

        // 🖼️ Profil Şəkli (SettingsController və SettingsVM üçün çatışmayan xassə)
        public string? ProfilePicturePath { get; set; }

        // Soft-delete, active flag and creation date used by admin pages
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Naviqasiya xassələri
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
        public virtual ICollection<Goal> Goals { get; set; } = new List<Goal>();
    }
}