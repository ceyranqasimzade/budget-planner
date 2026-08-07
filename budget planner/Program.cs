using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using budget_planner.DAL;
using budget_planner.Models;
using budget_planner.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(
    "http://0.0.0.0:5177",
    "https://0.0.0.0:7199"
);

// =========================================================================
// 1. DATABASE & IDENTITY SERVICES
// =========================================================================
builder.Services.AddDbContext<BudgetDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Şifrə tələbləri
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(15);
    })
    .AddEntityFrameworkStores<BudgetDbContext>()
    .AddDefaultTokenProviders();

// Identity Cookie Tənzimləmələri
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.Name = "BudgetPlannerAuthCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Giriş 7 gün aktiv qalsın
    options.SlidingExpiration = true;              // Hər müraciətdə yenilənsin
});

// =========================================================================
// 2. CONTROLLERS & JSON OPTIONS
// =========================================================================
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// =========================================================================
// 3. APPLICATION SERVICES (DEPENDENCY INJECTION)
// =========================================================================
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Session Tənzimləməsi
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Xüsusi Servislər
builder.Services.AddTransient<IEmailService, EmailService>();

// HttpClient ilə valyuta servisi
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();

// Business Logic Servisləri
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Əlavə olunan istifadəçi və avatar servisləri (Çatışmayan servislər):
builder.Services.AddScoped<IAvatarService, AvatarService>();
builder.Services.AddScoped<IUserDataCleanupService, UserDataCleanupService>();

// MƏHƏLLİ DİL TƏYİNATI (AZƏRBAYCAN DİLİ)
var defaultCulture = new CultureInfo("az-Latn-AZ");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.CurrencyDecimalSeparator = ".";

CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// =========================================================================
// 4. APPLICATION BUILD & MIDDLEWARE PIPELINE
// =========================================================================
var app = builder.Build();

// Lokallaşdırma Parametrləri (Ablan/Azərbaycan dili formatları üçün)
var customCulture = new CultureInfo("az-Latn-AZ");
customCulture.NumberFormat.NumberDecimalSeparator = ".";
customCulture.NumberFormat.CurrencyDecimalSeparator = ".";

var supportedCultures = new[]
{
    customCulture
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(customCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Brauzer dilinin (Accept-Language) Azərbaycan dilini üstələməsinin qarşısını almaq üçün təyin olunur:
localizationOptions.RequestCultureProviders.Clear();
localizationOptions.RequestCultureProviders.Add(new CustomRequestCultureProvider(context =>
{
    return Task.FromResult(new ProviderCultureResult("az-Latn-AZ"));
}));

app.UseRequestLocalization(localizationOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session və Auth sırası (Kritikdir)

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Route Təyini
app.MapControllerRoute(
     name: "areas",
     pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
     );
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

       
app.Run();