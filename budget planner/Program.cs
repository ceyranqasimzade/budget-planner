using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using budget_planner.DAL;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<BudgetDbContext>(options =>
{
    options.UseSqlServer("Server=.\\SQLEXPRESS;Database=TransactionDb;Trusted_Connection=True;TrustServerCertificate=True");
});
var app = builder.Build();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.Run();