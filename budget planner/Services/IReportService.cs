using budget_planner.Models;
using budget_planner.ViewModels.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace budget_planner.Services
{
    public interface IReportService
    {
        Task<ReportVM> GetReportDataAsync(
            string userId,
            ReportFilterVM? filter = null);

        Task<ReportVM> GetGuestReportDataAsync(
            List<Transaction> guestTransactions,
            ReportFilterVM? filter = null);
    }
}