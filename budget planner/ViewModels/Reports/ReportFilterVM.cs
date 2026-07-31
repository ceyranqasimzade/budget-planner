using System;

namespace budget_planner.ViewModels.Reports
{
    public class ReportFilterVM
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CardId { get; set; }
        public int? CategoryId { get; set; }
        public string DisplayCurrency { get; set; } = "AZN";
    }
}