using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels.Reports;
using budget_planner.Extensions;
namespace budget_planner.Controllers
{
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;
        public ReportController(IReportService reportService, UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string displayCurrency = "AZN")
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                var filter = new ReportFilterVM
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    DisplayCurrency = string.IsNullOrWhiteSpace(displayCurrency)
                        ? "AZN"
                        : displayCurrency
                };
                var guestTransactions = HttpContext.Session
                    .GetObject<List<Transaction>>("Guest_Transactions")
                    ?? new List<Transaction>();
                var report = await _reportService.GetGuestReportDataAsync(
                    guestTransactions,
                    filter);
                return View(report);
            }
            var defaultFilter = new ReportFilterVM
            {
                StartDate = startDate,
                EndDate = endDate,
                DisplayCurrency = string.IsNullOrWhiteSpace(displayCurrency) ? "AZN" : displayCurrency
            };
            var reportData = await _reportService.GetReportDataAsync(userId, defaultFilter);
            return View(reportData ?? new ReportVM());
        }
        [HttpGet]
        public async Task<IActionResult> GetReportData(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string displayCurrency = "AZN")
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                var filter = new ReportFilterVM
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    DisplayCurrency = string.IsNullOrWhiteSpace(displayCurrency)
                        ? "AZN"
                        : displayCurrency
                };
                var guestTransactions = HttpContext.Session
                    .GetObject<List<Transaction>>("Guest_Transactions")
                    ?? new List<Transaction>();
                var report = await _reportService.GetGuestReportDataAsync(
                    guestTransactions,
                    filter);
                return Json(report);
            }
            var defaultFilter = new ReportFilterVM
            {
                StartDate = startDate,
                EndDate = endDate,
                DisplayCurrency = string.IsNullOrWhiteSpace(displayCurrency) ? "AZN" : displayCurrency
            };
            var reportData = await _reportService.GetReportDataAsync(userId, defaultFilter);
            return Json(reportData);
        }
    }
}