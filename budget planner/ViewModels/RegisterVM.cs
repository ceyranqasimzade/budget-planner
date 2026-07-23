using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Ad və Soyad vacibdir")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "E-poçt vacibdir")]
        [EmailAddress(ErrorMessage = "Düzgün e-poçt daxil edin")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifrə vacibdir")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifrələr uyğun gəlmir")]
        public string ConfirmPassword { get; set; } = null!;
    }
}