using System;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class Goal : BaseModel
    {
        [Required(ErrorMessage = "Hədəf adı daxil edilməlidir")]
        [StringLength(100, ErrorMessage = "Hədəf adı maksimum 100 simvol ola bilər")]
        public string Title { get; set; } = null!;

        [Range(1, double.MaxValue, ErrorMessage = "Hədəf məbləği 0-dan böyük olmalıdır")]
        public decimal TargetAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cari məbləğ mənfi ola bilməz")]
        public decimal CurrentAmount { get; set; }

        [DataType(DataType.Date)]
        public DateTime Deadline { get; set; }

        // Dizayn üçün İkon və Rəng (İstəyə bağlı)
        public string IconClass { get; set; } = "bi-flag";
        public string ColorClass { get; set; } = "bg-primary";

        [Required]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;
    }
}