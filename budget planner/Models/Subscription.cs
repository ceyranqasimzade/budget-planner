using System;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class Subscription : BaseModel // Əgər BaseModel istifadə edirsinizsə
    {
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [Required(ErrorMessage = "Abunəlik adı daxil edilməlidir")]
        [StringLength(100)]
        public string Name { get; set; } = null!; // Məsələn: Netflix, İnternet

        public decimal Amount { get; set; }
        public DateTime NextPaymentDate { get; set; }

        public string? IconClass { get; set; } // Məsələn: bi-wifi, bi-display
        public string? ColorClass { get; set; } // Məsələn: bg-primary, bg-danger
    }
}