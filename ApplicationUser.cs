using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace budget_planner.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public BudgetRule? BudgetRule { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal CashBalance { get; set; }
        public string DefaultCurrency { get; set; }
        public string Theme { get; set; }
        public string Language { get; set; }
        public bool BudgetAlerts { get; set; }
        public bool EmailNotifications { get; set; }
        public string? ProfilePicturePath { get; set; }

        // Added properties used by the admin controller
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<Card> Cards { get; set; }
        public virtual ICollection<Goal> Goals { get; set; }
    }
}