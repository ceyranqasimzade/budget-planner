using System;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class TransactionCreateVM
    {
        // Nağd ödəniş seçildikdə boş (null) ola biləcəyi üçün [Required] silindi
        public int? CardId { get; set; }

        // Siyahıdan seçilən kateqoriya ID-si
        public int? CategoryId { get; set; }

        // Formadan daxil edilən və ya seçilən kateqoriya adı (köhnə sahəniz)
        public string? CategoryName { get; set; }

        // İstifadəçinin özünün yazdığı yeni kateqoriya adı
        public string? NewCategoryName { get; set; }

        [Required(ErrorMessage = "Məbləğ daxil edilməlidir")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Məbləğ 0-dan böyük olmalıdır")]
        public decimal Amount { get; set; }

        // Formadan seçilən valyuta (Məsələn: USD, EUR, AZN). Boş qalarsa default AZN götürüləcək.
        public string? Currency { get; set; } = "AZN";

        [Required(ErrorMessage = "Təsvir daxil edilməlidir")]
        [StringLength(200, ErrorMessage = "Təsvir maksimum 200 simvol ola bilər")]
        public string Description { get; set; } = null!;

        public bool IsIncome { get; set; }

        // Tarix sahəsi (ilkin dəyər cari vaxtı götürür)
        public DateTime Date { get; set; } = DateTime.Now;

        // Əlavə qeydlər
        public string? Note { get; set; }
    }
}