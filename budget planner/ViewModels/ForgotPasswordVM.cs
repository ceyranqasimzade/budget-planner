using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "E-poçt ünvanı vacibdir")]
        [EmailAddress(ErrorMessage = "Düzgün e-poçt ünvanı daxil edin")]
        public string Email { get; set; } = null!;
    }
}