using Microsoft.AspNetCore.Mvc;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Services;

namespace RemoteDesktopOnlineApps.Controllers
{
    public class ClientController : Controller
    {
        private readonly IClientIdentificationService _clientService;

        public ClientController(IClientIdentificationService clientService)
        {
            _clientService = clientService;
        }

        public async Task<IActionResult> Index()
        {
            // Register client on first run
            await _clientService.RegisterClientAsync();

            // Get connection info
            var connectionInfo = await _clientService.GetConnectionInfoAsync();

            return View(connectionInfo);
        }

        [HttpGet]
        [Route("api/client/ping")]
        public IActionResult Ping()
        {
            return Json(new { success = true, timestamp = DateTime.Now });
        }
    }
}