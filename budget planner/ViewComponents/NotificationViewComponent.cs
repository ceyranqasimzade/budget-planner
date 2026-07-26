using Microsoft.AspNetCore.Mvc;
using budget_planner.ViewModels; // Sənin modelinin olduğu namespace
using System.Collections.Generic;
using System.Threading.Tasks;

namespace budget_planner.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Gələcəkdə burada bazadan əsl bildirişləri çəkəcəksən.
            // Hələlik sənin NotificationVM modelinlə test məlumatları yaradırıq:
            var notifications = new List<NotificationVM>
            {
                new NotificationVM
                {
                    Title = "Büdcə Xəbərdarlığı!",
                    Message = "Hələ heç bir xərciniz yoxdur. Xərcləriniz yarandıqca sistem xəbərdarlıq edəcək.",
                    IconClass = "fa-solid fa-triangle-exclamation",
                    TextColorClass = "text-warning"
                },
                new NotificationVM
                {
                    Title = "Yeni Hədəf",
                    Message = "Yeni avtomobil üçün hədəfiniz uğurla yaradıldı.",
                    IconClass = "fa-solid fa-bullseye",
                    TextColorClass = "text-success"
                }
            };

            // Modeli Default.cshtml-ə göndəririk
            return View(notifications);
        }
    }
}