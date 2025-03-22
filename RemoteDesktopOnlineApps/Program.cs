using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Hubs;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Services;
var builder = WebApplication.CreateBuilder(args);
// تنظیم اتصال به دیتابیس
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
// تنظیم احراز هویت با کوکی (قبل از Build)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });
builder.Services.AddScoped<IdentityService>(); // ثبت سرویس
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 20 * 1024 * 1024; // 20MB
});
// ثبت سرویس‌های برنامه
builder.Services.AddScoped<IRemoteDesktopService, RemoteDesktopService>();
builder.Services.AddScoped<IWebRTCSignalingService, WebRTCSignalingService>();
builder.Services.AddScoped<IFileTransferService, FileTransferService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IClientIdentificationService, ClientIdentificationService>();
builder.Services.AddScoped<IRemoteDesktopStatsService, RemoteDesktopStatsService>();

builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<PasswordHasher<Users>>();
// افزودن HttpContextAccessor برای دسترسی به HttpContext
builder.Services.AddHttpContextAccessor();
// افزودن کنترلرها و ویوها
builder.Services.AddControllersWithViews();
// اضافه کردن سرویس سشن
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // تنظیم زمان انقضا
    options.Cookie.HttpOnly = true; // جلوگیری از دسترسی جاوااسکریپت به کوکی
    options.Cookie.IsEssential = true; // اطمینان از ضروری بودن کوکی
});
builder.Services.AddDistributedMemoryCache(); // مورد نیاز برای ذخیره‌سازی سشن
// افزودن حافظه نهان در حافظه
builder.Services.AddMemoryCache();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles(); // این خط اضافه شد برای فایل‌های استاتیک
app.UseSession();
app.UseRouting();
app.UseAuthentication(); // این خط اضافه شد - همیشه باید قبل از UseAuthorization باشد
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();
// پیکربندی هاب‌های SignalR
app.MapHub<RemoteSessionHub>("/remoteSessionHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<ConferenceHub>("/conferenceHub");
app.MapHub<NotificationHub>("/notificationHub");
await app.RunAsync();