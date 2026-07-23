using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class BudgetRule : BaseModel
    {
        [Required(ErrorMessage = "Ehtiyac faizi daxil edilməlidir")]
        [Range(0, 100, ErrorMessage = "Faiz 0 ilə 100 arasında olmalıdır")]
        public decimal NeedsPercentage { get; set; }



        [Required(ErrorMessage = "İstək faizi daxil edilməlidir")]
        [Range(0, 100, ErrorMessage = "Faiz 0 ilə 100 arasında olmalıdır")]
        public decimal WantsPercentage { get; set; }



        [Required(ErrorMessage = "Yığım faizi daxil edilməlidir")]
        [Range(0, 100, ErrorMessage = "Faiz 0 ilə 100 arasında olmalıdır")]
        public decimal SavingsPercentage { get; set; }



        [Required]
        public string UserId { get; set; } = null!;



        public ApplicationUser User { get; set; } = null!;
    }
}