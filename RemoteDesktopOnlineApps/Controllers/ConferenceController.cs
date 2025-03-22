using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using System.Security.Claims;

namespace RemoteDesktopOnlineApps.Controllers
{
    [Authorize]
    public class ConferenceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConferenceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int sessionId)
        {
            var session = await _context.RemoteSessions
                .Include(s => s.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return NotFound();

            return View(session);
        }

        [HttpPost]
        public async Task<IActionResult> Join(int sessionId)
        {
            var userId = User.GetUserId();
            var userName = User.Identity.Name;

            var participant = new SessionParticipant
            {
                RemoteSessionId = sessionId,
                UserId = userId,
                UserName = userName,
                JoinTime = System.DateTime.Now
            };

            _context.SessionParticipants.Add(participant);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { sessionId });
        }

        [HttpPost]
        public async Task<IActionResult> Leave(int sessionId)
        {
            var userId = User.GetUserId();

            var participant = await _context.SessionParticipants
                .FirstOrDefaultAsync(p => p.RemoteSessionId == sessionId && p.UserId == userId && !p.LeaveTime.HasValue);

            if (participant != null)
            {
                participant.LeaveTime = System.DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "RemoteSession");
        }

        [HttpGet]
        public async Task<IActionResult> GetParticipants(int sessionId)
        {
            var participants = await _context.SessionParticipants
                .Where(p => p.RemoteSessionId == sessionId && !p.LeaveTime.HasValue)
                .Select(p => new
                {
                    id = p.UserId,
                    userName = p.UserName,
                    joinTime = p.JoinTime
                })
                .ToListAsync();

            return Json(participants);
        }
    }
}
