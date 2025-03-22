using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Services;

namespace RemoteDesktopOnlineApps.Controllers
{
     
    public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IRemoteDesktopService _remoteDesktopService;

    public HomeController(
        ILogger<HomeController> logger,
        IRemoteDesktopService remoteDesktopService)
    {
        _logger = logger;
        _remoteDesktopService = remoteDesktopService;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        // در صفحه اصلی، مستقیماً به صفحه جلسات ریموت هدایت می‌شویم
        return RedirectToAction("Index", "RemoteSession");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    [Route("/speedtest.bin")]
    public IActionResult SpeedTest()
    {
        // تولید فایل باینری ۱۰۰ کیلوبایتی برای تست سرعت
        byte[] data = new byte[100 * 1024]; // 100KB
        new Random().NextBytes(data);

        return File(data, "application/octet-stream");
    }
}

}

