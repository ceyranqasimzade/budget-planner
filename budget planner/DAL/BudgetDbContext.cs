using budget_planner.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace budget_planner.DAL
{
    public class BudgetDbContext : IdentityDbContext<ApplicationUser>
    {
        public BudgetDbContext(
            DbContextOptions<BudgetDbContext> options)
            : base(options)
        {

        }


        public DbSet<Transaction> Transactions { get; set; } = null!;

        public DbSet<Category> Categories { get; set; } = null!;

        public DbSet<Card> Cards { get; set; } = null!;

        public DbSet<Goal> Goals { get; set; } = null!;

        public DbSet<BudgetRule> BudgetRules { get; set; } = null!;

        public DbSet<ExchangeRate> ExchangeRates { get; set; } = null!;

        // YENİ ƏLAVƏ EDİLDİ
        public DbSet<Subscription> Subscriptions { get; set; } = null!;




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);



            // ==========================
            // USER - TRANSACTION
            // One User -> Many Transactions
            // ==========================

            modelBuilder.Entity<Transaction>()
                .HasOne(x => x.User)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);



            // ==========================
            // CATEGORY - TRANSACTION
            // One Category -> Many Transactions
            // ==========================

            modelBuilder.Entity<Transaction>()
                .HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);



            // ==========================
            // CARD - TRANSACTION
            // One Card -> Many Transactions
            // CardId nullable
            // ==========================

            modelBuilder.Entity<Transaction>()
                .HasOne(x => x.Card)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CardId)
                .OnDelete(DeleteBehavior.SetNull);



            // ==========================
            // USER - CARD
            // One User -> Many Cards
            // ==========================

            modelBuilder.Entity<Card>()
                .HasOne(x => x.User)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);



            // ==========================
            // USER - GOAL
            // One User -> Many Goals
            // ==========================

            modelBuilder.Entity<Goal>()
                .HasOne(x => x.User)
                .WithMany(x => x.Goals)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);



            // ==========================
            // USER - SUBSCRIPTION (YENİ ƏLAVƏ EDİLDİ)
            // One User -> Many Subscriptions
            // ==========================

            modelBuilder.Entity<Subscription>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);



            // ==========================
            // USER - BUDGET RULE
            // One User -> One BudgetRule
            // ==========================

            modelBuilder.Entity<BudgetRule>()
                .HasOne(x => x.User)
                .WithOne(x => x.BudgetRule)
                .HasForeignKey<BudgetRule>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);




            // ==========================
            // DECIMAL PRECISION
            // ==========================

            modelBuilder.Entity<Transaction>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);



            modelBuilder.Entity<Card>()
                .Property(x => x.Balance)
                .HasPrecision(18, 2);



            modelBuilder.Entity<Goal>()
                .Property(x => x.TargetAmount)
                .HasPrecision(18, 2);



            modelBuilder.Entity<Goal>()
                .Property(x => x.CurrentAmount)
                .HasPrecision(18, 2);



            // YENİ ƏLAVƏ EDİLDİ
            modelBuilder.Entity<Subscription>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);



            modelBuilder.Entity<BudgetRule>()
                .Property(x => x.NeedsPercentage)
                .HasPrecision(5, 2);



            modelBuilder.Entity<BudgetRule>()
                .Property(x => x.WantsPercentage)
                .HasPrecision(5, 2);



            modelBuilder.Entity<BudgetRule>()
                .Property(x => x.SavingsPercentage)
                .HasPrecision(5, 2);



            modelBuilder.Entity<ExchangeRate>()
                .Property(x => x.Rate)
                .HasPrecision(18, 6);




            // ==========================
            // BUDGET RULE CHECK
            // Total percentage <= 100
            // ==========================

            modelBuilder.Entity<BudgetRule>()
                .ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_BudgetRule_TotalPercentage",
                        "NeedsPercentage + WantsPercentage + SavingsPercentage <= 100"
                    );
                });

        }
    }
}