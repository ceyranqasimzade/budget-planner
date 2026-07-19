using Microsoft.AspNetCore.Mvc;
using budget_planner.Models;
using System;
using System.Collections.Generic;
namespace budget_planner.Controllers
{
    public class TransactionController : Controller
    {
        public IActionResult Index()
        {
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    Id = 1,
                    Date = DateTime.Now.AddDays(-1),
                    Description = "Maaş",
                    Category = "Gəlir",
                    Amount = 1500.00m,
                    Status = "Tamamlandı",
                    IsIncome = true
                },
                new Transaction
                {
                    Id = 2,
                    Date = DateTime.Now,
                    Description = "Market alış-verişi",
                    Category = "Qida",
                    Amount = 45.50m,
                    Status = "Tamamlandı",
                    IsIncome = false
                }
            };
            return View(transactions);
        }
    }
}