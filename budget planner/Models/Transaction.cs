using System.ComponentModel.DataAnnotations;
namespace budget_planner.Models
{
    public class Transaction : BaseModel
    {
        [Required(ErrorMessage = "Təsvir daxil edilməlidir")]
        public string Description { get; set; } = null!;
        [Range(0, double.MaxValue,
            ErrorMessage = "Məbləğ mənfi ola bilməz")]
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool IsIncome { get; set; }
        public string Currency { get; set; } = "AZN";
        public string Status { get; set; } = "Tamamlandı";
        public int? CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int? CardId { get; set; }
        public Card? Card { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string? ReceiptUrl { get; set; } // Qəbzlərin/Çeklərin şəkli üçün
        public bool IsRecurring { get; set; } = false; // Təkrarlanan (abonəlik) ödənişdirmi?
        public string? RecurringFrequency { get; set; } // "Aylıq", "Həftəlik", "İllik"
    }
}