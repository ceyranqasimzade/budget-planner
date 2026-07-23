using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class ExchangeRate : BaseModel
    {
        [Required(ErrorMessage = "Başlanğıc valyuta seçilməlidir")]
        [StringLength(3, MinimumLength = 3,
            ErrorMessage = "Valyuta kodu 3 simvol olmalıdır")]
        public string FromCurrency { get; set; } = null!;



        [Required(ErrorMessage = "Hədəf valyuta seçilməlidir")]
        [StringLength(3, MinimumLength = 3,
            ErrorMessage = "Valyuta kodu 3 simvol olmalıdır")]
        public string ToCurrency { get; set; } = null!;



        [Range(0.0001, double.MaxValue,
            ErrorMessage = "Məzənnə 0-dan böyük olmalıdır")]
        public decimal Rate { get; set; }



        public DateTime Date { get; set; } = DateTime.Now;
    }
}