using System;
using Microsoft.AspNetCore.Http; // IFormFile üçün bu lazımdır

namespace budget_planner.ViewModels
{
    public class TransactionVM
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "AZN";
        public bool IsIncome { get; set; }

        public string? CategoryName { get; set; }
        public int? CategoryId { get; set; }

        public string? Status { get; set; }

        // --- KART PROBLƏMİNİ HƏLL EDƏN SƏTİRLƏR ---
        public int? CardId { get; set; }
        public string? CardName { get; set; }

        // --- QƏBZ VƏ ABUNƏLİK (RECURRING) SƏTİRLƏRİ ---
        public string? ReceiptUrl { get; set; }
        public IFormFile? ReceiptFile { get; set; } // <--- Yeni fayl yükləmək üçün

        public bool IsRecurring { get; set; }
        public string? RecurringFrequency { get; set; } // "Aylıq", "Həftəlik" və s.
    }
}