using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "İstifadəçi adı və ya e-poçt daxil etmək məcburidir")]
        public string UsernameOrEmail { get; set; } = null!;

        [Required(ErrorMessage = "Şifrə vacibdir")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; }
    }
}