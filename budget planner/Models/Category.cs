using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class Category : BaseModel
    {
        [Required(ErrorMessage = "Kateqoriya adı boş ola bilməz")]
        [StringLength(50, ErrorMessage = "Kateqoriya adı maksimum 50 simvol ola bilər")]
        public string Name { get; set; } = null!;

        public string Icon { get; set; } = "default.png";

        [Required(ErrorMessage = "Tip seçilməlidir")]
        public string Type { get; set; } = null!;

        public string? BudgetType { get; set; }

        // --- İSTİFADƏÇİ ƏLAQƏSİ ---
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
            = new List<Transaction>();
    }
}