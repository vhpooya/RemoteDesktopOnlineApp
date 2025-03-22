using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // نمایش صفحه اصلی چت
        public async Task<IActionResult> Index(int? sessionId)
        {
            var userId = User.GetUserId();

            if (sessionId.HasValue)
            {
                // بررسی دسترسی کاربر به جلسه
                var hasAccess = await _context.RemoteSessions
                    .AnyAsync(s => s.Id == sessionId.Value &&
                        (s.UserId == userId || s.Participants.Any(p => p.UserId == userId)));

                if (!hasAccess)
                {
                    return Forbid();
                }

                ViewBag.SessionId = sessionId.Value;

                // دریافت پیام‌های جلسه
                var messages = await _context.ChatMessages
                    .Where(m => m.RemoteSessionId == sessionId.Value)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                // دریافت اطلاعات جلسه
                var session = await _context.RemoteSessions
                    .Include(s => s.User)
                    .Include(s => s.Participants)
                    .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(s => s.Id == sessionId.Value);

                ViewBag.Session = session;

                return View(messages);
            }
            else
            {
                // نمایش لیست جلسات که کاربر در آنها حضور دارد
                var sessions = await _context.RemoteSessions
                    .Where(s => s.UserId == userId || s.Participants.Any(p => p.UserId == userId))
                    .OrderByDescending(s => s.StartTime)
                    .Include(s => s.User)
                    .Select(s => new ChatSessionViewModel
                    {
                        SessionId = s.Id,
                        SessionName = s.ServerName,
                        StartTime = s.StartTime,
                        Status = s.Status,
                        UnreadCount = _context.ChatMessages
                            .Count(m => m.RemoteSessionId == s.Id && m.Timestamp > DateTime.Now.AddDays(-1)),
                        LastMessage = _context.ChatMessages
                            .Where(m => m.RemoteSessionId == s.Id)
                            .OrderByDescending(m => m.Timestamp)
                            .Select(m => new { m.Message, m.Timestamp })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return View("SessionList", sessions);
            }
        }

        // ارسال پیام جدید
        [HttpPost]
        public async Task<IActionResult> Send(int sessionId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("پیام نمی‌تواند خالی باشد");
            }

            var userId = User.GetUserId();
            var userName = User.Identity.Name;

            // بررسی دسترسی کاربر به جلسه
            var hasAccess = await _context.RemoteSessions
                .AnyAsync(s => s.Id == sessionId &&
                    (s.UserId == userId || s.Participants.Any(p => p.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // ایجاد پیام جدید
            var chatMessage = new ChatMessage
            {
                RemoteSessionId = sessionId,
                SenderId = userId,
                SenderName = userName,
                Message = message,
                Timestamp = DateTime.Now,
                IsFromSupport = User.IsInRole("Support") // تشخیص کارشناس پشتیبانی
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // اگر درخواست Ajax است
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    id = chatMessage.Id,
                    senderId = chatMessage.SenderId,
                    senderName = chatMessage.SenderName,
                    message = chatMessage.Message,
                    timestamp = chatMessage.Timestamp,
                    isFromSupport = chatMessage.IsFromSupport
                });
            }

            return RedirectToAction(nameof(Index), new { sessionId });
        }

        // دریافت پیام‌های جدید (برای Ajax)
        [HttpGet]
        public async Task<IActionResult> GetMessages(int sessionId, DateTime? since = null)
        {
            var userId = User.GetUserId();

            // بررسی دسترسی کاربر به جلسه
            var hasAccess = await _context.RemoteSessions
                .AnyAsync(s => s.Id == sessionId &&
                    (s.UserId == userId || s.Participants.Any(p => p.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // دریافت پیام‌های جدید
            var query = _context.ChatMessages
                .Where(m => m.RemoteSessionId == sessionId);

            if (since.HasValue)
            {
                query = query.Where(m => m.Timestamp > since.Value);
            }

            var messages = await query
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    senderName = m.SenderName,
                    message = m.Message,
                    timestamp = m.Timestamp,
                    isFromSupport = m.IsFromSupport
                })
                .ToListAsync();

            return Json(messages);
        }

        // حذف پیام
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            // یافتن پیام و بررسی دسترسی
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == id && m.SenderId == userId);

            if (message == null)
            {
                return NotFound();
            }

            // حذف پیام
            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();

            // اگر درخواست Ajax است
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }

            return RedirectToAction(nameof(Index), new { sessionId = message.RemoteSessionId });
        }
    }

    // ViewModel برای نمایش لیست جلسات چت
    public class ChatSessionViewModel
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public DateTime StartTime { get; set; }
        public string Status { get; set; }
        public int UnreadCount { get; set; }
        public object LastMessage { get; set; }
    }
}