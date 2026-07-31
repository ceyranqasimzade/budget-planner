using budget_planner.Models;
using budget_planner.Services;
using budget_planner.ViewModels.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Index(ReportFilterVM filter)
        {
            var user = await _userManager.GetUserAsync(User);

            // YENİLİK: Başqa heç nəyə toxunmadan, Session-da olan GuestUserId-ni də yoxlayırıq:
            string userId = user?.Id
                            ?? HttpContext.Session.GetString("GuestUserId")
                            ?? GetOrCreateGuestId();

            var vm = await _reportService.GetReportDataAsync(userId, filter);

            // DEBUG: Bazadan məlumat gəlib-gəlmədiyini Visual Studio 'Output' panelində görmək üçün:
            System.Diagnostics.Debug.WriteLine("=== DEBUG MƏLUMATLAR ===");
            System.Diagnostics.Debug.WriteLine($"İstifadəçi ID: {userId}");
            System.Diagnostics.Debug.WriteLine($"Aylıq Gəlir: {vm.Kpi.MonthlyIncome}");
            System.Diagnostics.Debug.WriteLine($"Aylıq Xərc: {vm.Kpi.MonthlyExpense}");

            return View(vm);
        }

        private string GetOrCreateGuestId()
        {
            // Qonaq istifadəçi üçün brauzer Sessiya ID-si götürülür
            string guestId = HttpContext.Session.GetString("GuestUserId");
            if (string.IsNullOrEmpty(guestId))
            {
                guestId = "guest_" + System.Guid.NewGuid().ToString();
                HttpContext.Session.SetString("GuestUserId", guestId);
            }
            return guestId;
        }
    }
}