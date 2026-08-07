using System;
using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class UpcomingPayment
    {
        public int Id { get; set; }

        // İstifadəçi ilə əlaqə
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public string Title { get; set; } = null!; // Ödənişin adı

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "AZN";

        public DateTime DueDate { get; set; }

        public bool IsPaid { get; set; } = false;

        // ===============================
        // Təkrarlanan ödəniş üçün
        // ===============================

        public bool IsRecurring { get; set; } = false;
  
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    }
}