using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class ResetPasswordVM
    {
        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;

        [Required(ErrorMessage = "Yeni şifrə vacibdir")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifrələr eyni deyil")]
        public string ConfirmPassword { get; set; } = null!;
    }
}