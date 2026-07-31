using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.Services;
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

// Servislərin qeydiyyatı
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

// HttpClient və Scoped servislər
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReportService, ReportService>();

// 🟢 SESSION SERVİSLƏRİ
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // 2 saatlıq müvəqqəti yaddaş
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Lokallaşdırma parametrləri
var customCulture = new CultureInfo("az-Latn-AZ");
customCulture.NumberFormat.NumberDecimalSeparator = ".";
customCulture.NumberFormat.CurrencyDecimalSeparator = ".";

var supportedCultures = new[] { customCulture };

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(customCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 🟢 ROUTING, SESSION VƏ AUTHENTICATION MIDDLEWARE-LƏRİ
app.UseRouting();

app.UseSession(); // <-- Session Middleware

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();