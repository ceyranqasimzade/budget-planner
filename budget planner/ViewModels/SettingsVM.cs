using System.ComponentModel.DataAnnotations;

namespace budget_planner.ViewModels
{
    public class SettingsVM
    {
        // 👤 HESAB MƏLUMATLARI
        [Display(Name = "Ad Soyad")]
        [StringLength(100, ErrorMessage = "{0} maksimum {1} simvol ola bilər.")]
        public string? FullName { get; set; }

        [Display(Name = "E-poçt ünvanı")]
        [EmailAddress(ErrorMessage = "Düzgün bir e-poçt ünvanı daxil edin.")]
        public string? Email { get; set; }

        [Display(Name = "Profil Şəkli")]
        public string? ProfilePicturePath { get; set; }

        // 🔑 ŞİFRƏ DƏYİŞMƏ
        [Display(Name = "Cari Şifrə")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "Yeni Şifrə")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} ən azı {2} simvol olmalıdır.")]
        public string? NewPassword { get; set; }

        [Display(Name = "Yeni Şifrə (Təkrar)")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Yeni şifrə ilə təkrar şifrə üst-üstə düşmür.")]
        public string? ConfirmPassword { get; set; }

        // ⚙️ TƏTBİQ PARAMETRLƏRİ
        [Required(ErrorMessage = "Zəhmət olmasa valyuta seçin.")]
        public string DefaultCurrency { get; set; } = "AZN";

        [Required(ErrorMessage = "Zəhmət olmasa dili seçin.")]
        public string Language { get; set; } = "AZ";

        // 🔔 BİLDİRİŞLƏR
        public bool EmailNotifications { get; set; } = true;
        public bool BudgetAlerts { get; set; } = true;
    }
}