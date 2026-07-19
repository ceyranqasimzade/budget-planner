using budget_planner.Models;
using System;
using System.ComponentModel.DataAnnotations;
namespace budget_planner.Models
{
    public class Transaction : BaseModel
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]
        [StringLength(100, ErrorMessage = "Təsvir maksimum 100 hərf ola bilər.")]
        public string Description { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "Kateqoriya maksimum 50 hərf ola bilər.")]
        public string Category { get; set; }
        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "Məbləğ 0-dan böyük olmalıdır.")]
        public decimal Amount { get; set; }
        [Required]
        [StringLength(30, ErrorMessage = "Status maksimum 30 hərf ola bilər.")]
        public string Status { get; set; }
        [Required]
        public bool IsIncome { get; set; }
    }
}