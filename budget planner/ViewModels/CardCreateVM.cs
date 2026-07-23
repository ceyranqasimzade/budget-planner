using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class CardCreateVM
    {
        [Required(ErrorMessage = "Kart adı daxil edilməlidir")]
        [StringLength(50, ErrorMessage = "Kart adı maksimum 50 simvol ola bilər")]
        public string CardName { get; set; } = null!;

        [Required(ErrorMessage = "Son 4 rəqəm daxil edilməlidir")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Kartın son 4 rəqəmi dəqiq 4 simvol olmalıdır")]
        public string Last4Digits { get; set; } = null!;

        [Required(ErrorMessage = "Valyuta seçilməlidir")]
        public string Currency { get; set; } = "AZN";

        [Range(0, double.MaxValue, ErrorMessage = "Balans mənfi ola bilməz")]
        public decimal Balance { get; set; }
    }
}