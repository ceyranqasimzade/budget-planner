using System.ComponentModel.DataAnnotations;

namespace budget_planner.Areas.ViewModel
{
    public class CreateVM
    {
        // For update scenarios the Id and IsActive values are needed
        public string Id { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        [Required(ErrorMessage = "Ad və Soyad tələb olunur")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "İstifadəçi adı tələb olunur")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-poçt tələb olunur")]
        [EmailAddress(ErrorMessage = "Yanlış e-poçt ünvanı")]
        public string Email { get; set; } = string.Empty;
    }
}