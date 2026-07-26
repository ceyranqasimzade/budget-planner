using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace budget_planner.Models
{
    public class UpcomingPayment
    {
        public int Id { get; set; }

        // İstifadəçi ilə əlaqə
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public string Title { get; set; } = null!; // Ödənişin adı (məs: İnternet, Netflix, Kredit)

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "AZN";

        public DateTime DueDate { get; set; } // Ödənişin son tarixi

        public bool IsPaid { get; set; } = false; // Ödənilibmi? (Bəli/Xeyr)
    }
}