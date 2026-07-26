using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.Services; // YENİ: Servisi tanıması üçün namespace əlavə olundu
using System.Globalization;
using Microsoft.AspNetCore.Localization;

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
// YENİ: MemoryCache servisi qeydiyyatdan keçirilir
// ==========================================
builder.Services.AddMemoryCache();

// ==========================================
// YENİ: Valyuta servisini qeydiyyatdan keçiririk
// ==========================================
builder.Services.AddHttpClient<CurrencyService>();

var app = builder.Build();

// ==========================================
// LOKALİZASİYA VƏ ONDALIQ AYIRICI TƏNZİMLƏMƏLƏRİ
// ==========================================
// Azərbaycan mədəniyyət (culture) ayarlarını yaradırıq
var customCulture = new CultureInfo("az-Latn-AZ");

// Qəpikləri (ondalıq hissəni) nöqtə (.) ilə qəbul etməsi üçün məcbur edirik
customCulture.NumberFormat.NumberDecimalSeparator = ".";
customCulture.NumberFormat.CurrencyDecimalSeparator = ".";

var supportedCultures = new[] { customCulture };

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(customCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
// ==========================================

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