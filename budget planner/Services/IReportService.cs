using budget_planner.ViewModels.Reports;
using System.Threading.Tasks;

namespace budget_planner.Services
{
    public interface IReportService
    {
        Task<ReportVM> GetReportDataAsync(string userId, ReportFilterVM filter = null);
    }
}