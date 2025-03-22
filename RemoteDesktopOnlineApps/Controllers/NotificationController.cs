using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopOnlineApps.Helpers;
using RemoteDesktopOnlineApps.Services;
using System.Security.Claims;

namespace RemoteDesktopOnlineApps.Controllers
{
    /// <summary>
    /// کنترلر مدیریت اعلان‌ها
    /// </summary>
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;

        /// <summary>
        /// سازنده کنترلر اعلان‌ها
        /// </summary>
        /// <param name="notificationService">سرویس اعلان‌ها</param>
        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// نمایش صفحه اعلان‌ها
        /// </summary>
        /// <returns>ویو</returns>
        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, false, 50);
            return View(notifications);
        }

        /// <summary>
        /// دریافت اعلان‌های خوانده‌نشده
        /// </summary>
        /// <returns>لیست اعلان‌ها</returns>
        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var userId = User.GetUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, true, 10);

            return Json(notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                timestamp = n.Timestamp,
                data = n.Data
            }));
        }

        /// <summary>
        /// علامت‌گذاری یک اعلان به‌عنوان خوانده‌شده
        /// </summary>
        /// <param name="id">شناسه اعلان</param>
        /// <returns>وضعیت</returns>
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = result });
        }

        /// <summary>
        /// علامت‌گذاری همه اعلان‌ها به‌عنوان خوانده‌شده
        /// </summary>
        /// <returns>تعداد اعلان‌های بروزرسانی‌شده</returns>
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true, count });
        }

        /// <summary>
        /// حذف یک اعلان
        /// </summary>
        /// <param name="id">شناسه اعلان</param>
        /// <returns>وضعیت</returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _notificationService.DeleteNotificationAsync(id);
            return Json(new { success = result });
        }

        /// <summary>
        /// حذف همه اعلان‌های خوانده‌شده
        /// </summary>
        /// <returns>تعداد اعلان‌های حذف‌شده</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteRead()
        {
            var userId = User.GetUserId();
            var count = await _notificationService.DeleteReadNotificationsAsync(userId);
            return Json(new { success = true, count });
        }
    }
}
