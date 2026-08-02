using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace budget_planner.Models
{
    public class Goal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hədəfin adı mütləq qeyd olunmalıdır")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Title xətasının qarşısını almaq üçün Name-ə yönləndiririk
        [NotMapped]
        public string Title
        {
            get => Name;
            set => Name = value;
        }

        public decimal CurrentAmount { get; set; } = 0;

        [Required(ErrorMessage = "Hədəf məbləği mütləq qeyd olunmalıdır")]
        public decimal TargetAmount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AZN";

        [MaxLength(50)]
        public string IconClass { get; set; } = "bi-house-heart-fill";

        [MaxLength(50)]
        public string ColorClass { get; set; } = "primary";

        public DateTime? Deadline { get; set; }

        // Soft delete xətasının qarşısını almaq üçün
        public bool IsDeleted { get; set; } = false;

        public string UserId { get; set; } = string.Empty;

        // MÜTLƏQ ApplicationUser olmalıdır
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}