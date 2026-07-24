using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class TransactionCreateVM
    {
        // Nağd ödəniş seçildikdə boş (null) ola biləcəyi üçün [Required] silindi
        public int? CardId { get; set; }

        // Formadan daxil edilən və ya seçilən kateqoriya adı
        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Məbləğ daxil edilməlidir")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Məbləğ 0-dan böyük olmalıdır")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Təsvir daxil edilməlidir")]
        [StringLength(200, ErrorMessage = "Təsvir maksimum 200 simvol ola bilər")]
        public string Description { get; set; } = null!;

        public bool IsIncome { get; set; }
    }
}