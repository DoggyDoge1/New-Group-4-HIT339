using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Habibalib.Data;
using Habibalib.Security;
using Habibalib.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

// This group assignment is educational use under the QuestPDF Community license.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<CatalogueService>();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddSingleton<ReportExportService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Habibalib read-only catalogue API", Version = "v1",
        Description = "Available catalogue items, categories and configured operating status. Supply X-Api-Key. Branch filters apply to books only." });
    options.DocInclusionPredicate((_, description) => description.RelativePath?.StartsWith("api/v1/") == true);
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "X-Api-Key",
        Description = "API key configured by the administrator."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }] = Array.Empty<string>()
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("catalogue", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<Habibalib.Exceptions.KioskExceptionFilter>();
builder.Services.AddScoped<Habibalib.Exceptions.BranchTransferExceptionFilter>();
builder.Services.AddHostedService<Habibalib.Services.NotificationBackgroundService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=libdata.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
    ApiKeyAuthenticationHandler.SchemeName, _ => { });
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

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Habibalib API v1"));
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// unknown URLs under /Kiosk get the kiosk-styled 404 page
app.MapControllerRoute(
    name: "kioskNotFound",
    pattern: "Kiosk/{**path}",
    defaults: new { controller = "Kiosk", action = "Error", code = 404 });

app.Run();
