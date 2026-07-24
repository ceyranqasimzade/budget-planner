using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.Services; // YENİ: Servisi tanıması üçün namespace əlavə olundu

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BudgetDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<BudgetDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

// ==========================================
// YENİ: Valyuta servisini qeydiyyatdan keçiririk
// ==========================================
builder.Services.AddHttpClient<CurrencyService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();