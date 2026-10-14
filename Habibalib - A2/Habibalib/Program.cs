using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Habibalib.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<Habibalib.Exceptions.KioskExceptionFilter>();
builder.Services.AddScoped<Habibalib.Exceptions.BranchTransferExceptionFilter>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=libdata.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// make db and add starting data first time app runs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Run(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// unknown URLs under /Kiosk get the kiosk-styled 404 page
app.MapControllerRoute(
    name: "kioskNotFound",
    pattern: "Kiosk/{**path}",
    defaults: new { controller = "Kiosk", action = "Error", code = 404 });

app.Run();
