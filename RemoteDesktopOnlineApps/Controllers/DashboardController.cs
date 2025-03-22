using Microsoft.AspNetCore.Mvc;
using RemoteDesktopOnlineApps.Models;

namespace RemoteDesktopOnlineApps.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dashboard/ClientOverview/5
        public async Task<IActionResult> ClientOverview(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            // Get the most recent server metrics for this client
            var latestMetrics = await _context.ServerMetrics
                .Where(m => m.ClientId == id)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            // Get historical data for charts (last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var historicalData = await _context.ServerMetrics
                .Where(m => m.ClientId == id && m.Timestamp >= thirtyDaysAgo)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            ViewBag.Client = client;
            ViewBag.LatestMetrics = latestMetrics;
            ViewBag.HistoricalData = historicalData;

            return View();
        }

        // GET: Dashboard/Index
        public async Task<IActionResult> Index()
        {
            // Get all active clients
            var activeClients = await _context.Clients
                .Where(c => c.IsActive)
                .ToListAsync();

            // Get latest metrics for each active client
            var clientMetrics = new Dictionary<int, ServerMetrics>();
            foreach (var client in activeClients)
            {
                var latestMetric = await _context.ServerMetrics
                    .Where(m => m.ClientId == client.Id)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                if (latestMetric != null)
                {
                    clientMetrics[client.Id] = latestMetric;
                }
            }

            ViewBag.ActiveClients = activeClients;
            ViewBag.ClientMetrics = clientMetrics;

            return View();
        }
    }
}
