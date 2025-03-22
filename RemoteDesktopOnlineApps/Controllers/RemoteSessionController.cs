using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Services;
using RemoteDesktopOnlineApps.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Controllers
{
    [Authorize]
    public class RemoteSessionController : Controller
    {
        private readonly IRemoteDesktopService _remoteDesktopService;
        private readonly IRemoteDesktopStatsService _statsService;
        private readonly ApplicationDbContext _context;

        public RemoteSessionController(
            IRemoteDesktopService remoteDesktopService,
            IRemoteDesktopStatsService statsService,
            ApplicationDbContext context)
        {
            _remoteDesktopService = remoteDesktopService;
            _statsService = statsService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            var recentSessions = await _remoteDesktopService.GetRecentSessionsAsync(userId);
            return View(recentSessions);
        }

        public IActionResult Connect()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Connect(string serverIdentifier, string serverName, string password, bool savePassword = false)
        {
            var userId = User.GetUserId();

            var session = await _remoteDesktopService.InitiateSessionAsync(userId, serverIdentifier, serverName,password);

            bool connected = await _remoteDesktopService.ConnectAsync(session.Id, password);

            if (connected)
            {
                if (savePassword)
                {
                    await _remoteDesktopService.SaveSessionCredentialsAsync(session.Id, password);
                }

                return RedirectToAction("Session", new { id = session.Id });
            }

            ModelState.AddModelError("", "خطا در برقراری ارتباط با سرور. لطفاً اطلاعات ورود را بررسی کنید.");
            return View();
        }

        public async Task<IActionResult> Session(int id)
        {
            var session = await _context.RemoteSessions
                .Include(s => s.User)
                .Include(s => s.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            // دریافت آخرین آمار اتصال برای نمایش در صفحه
            var stats = await _statsService.GetConnectionStatsAsync(session.SessionId);

            // ایجاد ViewModel برای انتقال داده‌ها به ویو
            var viewModel = new RemoteSessionViewModel
            {
                Session = session,
                ConnectionStats = stats
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Disconnect(int id)
        {
            await _remoteDesktopService.DisconnectAsync(id);

            // توقف مانیتورینگ آمار
            var session = await _context.RemoteSessions.FindAsync(id);
            if (session != null)
            {
                await _statsService.StopMonitoringSessionAsync(session.SessionId);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SaveCredentials(int id, string password)
        {
            await _remoteDesktopService.SaveSessionCredentialsAsync(id, password);
            return RedirectToAction("Session", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> InviteParticipant(int sessionId, int participantId)
        {
            var success = await _remoteDesktopService.InviteParticipantAsync(sessionId, participantId);
            return Json(new { success });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userId = User.GetUserId();
            var recentSessions = await _remoteDesktopService.GetRecentSessionsAsync(userId);

            var result = recentSessions.Select(s => new {
                id = s.Id,
                serverIdentifier = s.ServerIdentifier,
                serverName = s.ServerName,
                lastConnected = s.StartTime,
                isPasswordSaved = s.IsPasswordSaved,
                status = s.Status
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> ConnectWithSaved(string serverId)
        {
            var userId = User.GetUserId();

            // پیدا کردن آخرین جلسه با این سرور
            var lastSession = await _context.RemoteSessions
                .Where(s => s.UserId == userId && s.ServerIdentifier == serverId && s.IsPasswordSaved)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync();

            if (lastSession == null)
                return RedirectToAction("Connect");

            // ایجاد جلسه جدید
            var newSession = await _remoteDesktopService.InitiateSessionAsync(
                userId, lastSession.ServerIdentifier, lastSession.ServerName, lastSession.SavedPasswordHash);

            // دریافت رمز عبور ذخیره شده
            var savedPassword = await _remoteDesktopService.GetSavedPasswordAsync(lastSession.Id);

            if (string.IsNullOrEmpty(savedPassword))
                return RedirectToAction("Connect");

            // اتصال به سرور
            bool connected = await _remoteDesktopService.ConnectAsync(newSession.Id, savedPassword);

            if (connected)
            {
                // ذخیره رمز عبور در جلسه جدید
                await _remoteDesktopService.SaveSessionCredentialsAsync(newSession.Id, savedPassword);
                return RedirectToAction("Session", new { id = newSession.Id });
            }

            return RedirectToAction("Connect");
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            var currentUserId = User.GetUserId();

            var users = await _context.Users
                .Where(u => u.Id != currentUserId &&
                           (u.UserName.Contains(term) || u.FullName.Contains(term)))
                .Select(u => new {
                    id = u.Id,
                    username = u.UserName,
                    fullName = u.FullName
                })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetConnectionStats(int sessionId)
        {
            var stats = await _statsService.GetConnectionStatsAsync(sessionId);
            return Json(stats);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConnectionStats(int sessionId, [FromBody] RemoteConnectionStats stats)
        {
            if (stats == null)
                return BadRequest();

            await _statsService.UpdateConnectionStatsAsync(sessionId, stats);
            return Ok();
        }
    }
}