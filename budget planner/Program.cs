using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using budget_planner.DAL;
using budget_planner.Models;


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