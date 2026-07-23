using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "E-poçt vacibdir")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifrə vacibdir")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; }
    }
}