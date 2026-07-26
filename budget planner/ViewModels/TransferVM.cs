using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class TransferVM
    {
        [Required(ErrorMessage = "Göndərən kart seçilməlidir")]
        public int FromCardId { get; set; }

        [Required(ErrorMessage = "Alan kart seçilməlidir")]
        public int ToCardId { get; set; }

        [Required(ErrorMessage = "Məbləğ daxil edilməlidir")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Məbləğ 0-dan böyük olmalıdır")]
        public decimal Amount { get; set; }

        // Controller-də istifadə olunan Currency xassəsi əlavə olundu:
        public string? Currency { get; set; }
    }
}