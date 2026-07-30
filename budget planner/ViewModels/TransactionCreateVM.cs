using System;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class TransactionCreateVM
    {
        [Required(ErrorMessage = "Məbləğ daxil edilməlidir.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Məbləğ 0-dan böyük olmalıdır.")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public bool IsIncome { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Now;

        public string? Currency { get; set; } = "AZN";

        // Kart seçimi üçün
        public int? CardId { get; set; }

        // Mövcud kateqoriya seçimi üçün
        public int? CategoryId { get; set; }

        // Birbaşa ad yazmaq və ya yeni kateqoriya əlavə etmək üçün
        public string? CategoryName { get; set; }
        public string? NewCategoryName { get; set; }

        // Status seçimi (Varsayılan: Tamamlandı)
        public string Status { get; set; } = "Tamamlandı";
    }
}