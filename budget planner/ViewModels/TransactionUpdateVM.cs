using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace budget_planner.ViewModels
{
    public class TransactionUpdateVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Məbləğ qeyd edilməlidir")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Məbləğ 0-dan böyük olmalıdır")]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "AZN";

        public string? Description { get; set; }

        [Required(ErrorMessage = "Tarix seçilməlidir")]
        public DateTime Date { get; set; }

        public bool IsIncome { get; set; }

        public string? Status { get; set; }

        // Redaktə edərkən yalnız mövcud kateqoriyalardan seçilməsi üçün (Variant 2)
        public int? CategoryId { get; set; }

        public int? CardId { get; set; }

        // Köhnə (hazırda bazada olan) qəbz faylının yolunu View-da göstərmək üçün
        public string? ExistingReceiptUrl { get; set; }

        // Əgər istifadəçi redaktə edərkən yeni qəbz faylı yükləsə, o bura düşəcək
        public IFormFile? NewReceiptFile { get; set; }

        public bool IsRecurring { get; set; }

        public string? RecurringFrequency { get; set; }
    }
}