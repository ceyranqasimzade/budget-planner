using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class TransactionCreateVM
    {
        [Required(ErrorMessage = "Məbləğ qeyd edilməlidir.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Məbləğ 0-dan böyük olmalıdır.")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public bool IsIncome { get; set; }

        public int? CategoryId { get; set; }
        public string? NewCategoryName { get; set; }

        public string? Currency { get; set; }
        public int? CardId { get; set; }
        public string? Status { get; set; }

        // YENİ ƏLAVƏ EDİLƏNLƏR:
        public IFormFile? ReceiptFile { get; set; } // Faylı formdan qəbul etmək üçün
        public bool IsRecurring { get; set; }
        public string? RecurringFrequency { get; set; } // Məsələn: "Daily", "Weekly", "Monthly"
    }
}