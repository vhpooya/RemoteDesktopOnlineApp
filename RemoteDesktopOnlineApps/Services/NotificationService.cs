using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RemoteDesktopOnlineApps.Hubs;
using RemoteDesktopOnlineApps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Services
{
    /// <summary>
    /// سرویس مدیریت اعلان‌ها و هشدارها
    /// </summary>
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> notificationHub,
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _notificationHub = notificationHub;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// ارسال اعلان به یک کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="title">عنوان اعلان</param>
        /// <param name="message">متن اعلان</param>
        /// <param name="type">نوع اعلان</param>
        /// <param name="saveToDatabase">ذخیره در دیتابیس</param>
        /// <returns>Task</returns>
        public async Task SendToUserAsync(int userId, string title, string message, NotificationType type, bool saveToDatabase = true)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type.ToString(),
                    Timestamp = DateTime.Now,
                    IsRead = false
                };

                // ارسال به کاربر از طریق SignalR
                await _notificationHub.Clients.Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", notification);

                // ذخیره در دیتابیس اگر لازم باشد
                if (saveToDatabase)
                {
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"اعلان ارسال شد به کاربر {userId}: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در ارسال اعلان به کاربر {userId}");
            }
        }

        /// <summary>
        /// ارسال اعلان به گروهی از کاربران
        /// </summary>
        /// <param name="groupName">نام گروه</param>
        /// <param name="title">عنوان اعلان</param>
        /// <param name="message">متن اعلان</param>
        /// <param name="type">نوع اعلان</param>
        /// <param name="saveToDatabase">ذخیره در دیتابیس</param>
        /// <returns>Task</returns>
        public async Task SendToGroupAsync(string groupName, string title, string message, NotificationType type, bool saveToDatabase = true)
        {
            try
            {
                var notification = new Notification
                {
                    GroupName = groupName,
                    Title = title,
                    Message = message,
                    Type = type.ToString(),
                    Timestamp = DateTime.Now,
                    IsRead = false
                };

                // ارسال به گروه از طریق SignalR
                await _notificationHub.Clients.Group($"notification_{groupName}")
                    .SendAsync("ReceiveNotification", notification);

                // ذخیره در دیتابیس اگر لازم باشد
                if (saveToDatabase)
                {
                    // دریافت کاربران گروه
                    var usersInGroup = await _context.GroupMembers
                        .Where(g => g.GroupName == groupName)
                        .Select(g => g.UserId)
                        .ToListAsync();

                    // ایجاد یک اعلان برای هر کاربر در گروه
                    foreach (var userId in usersInGroup)
                    {
                        var userNotification = new Notification
                        {
                            UserId = userId,
                            GroupName = groupName,
                            Title = title,
                            Message = message,
                            Type = type.ToString(),
                            Timestamp = DateTime.Now,
                            IsRead = false
                        };

                        _context.Notifications.Add(userNotification);
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"اعلان ارسال شد به گروه {groupName}: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در ارسال اعلان به گروه {groupName}");
            }
        }

        /// <summary>
        /// ارسال اعلان به همه کاربران
        /// </summary>
        /// <param name="title">عنوان اعلان</param>
        /// <param name="message">متن اعلان</param>
        /// <param name="type">نوع اعلان</param>
        /// <param name="saveToDatabase">ذخیره در دیتابیس</param>
        /// <returns>Task</returns>
        public async Task SendToAllAsync(string title, string message, NotificationType type, bool saveToDatabase = true)
        {
            try
            {
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type.ToString(),
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    IsGlobal = true
                };

                // ارسال به همه کاربران از طریق SignalR
                await _notificationHub.Clients.All
                    .SendAsync("ReceiveNotification", notification);

                // ذخیره در دیتابیس اگر لازم باشد
                if (saveToDatabase)
                {
                    // ایجاد یک اعلان سراسری
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"اعلان ارسال شد به همه کاربران: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ارسال اعلان به همه کاربران");
            }
        }

        /// <summary>
        /// ارسال اعلان درخواست ریموت دسکتاپ
        /// </summary>
        /// <param name="serverIdentifier">شناسه سرور</param>
        /// <param name="requesterName">نام درخواست‌کننده</param>
        /// <param name="requesterId">شناسه درخواست‌کننده</param>
        /// <returns>Task</returns>
        public async Task SendRemoteRequestAsync(string serverIdentifier, string requesterName, int requesterId)
        {
            try
            {
                var notification = new Notification
                {
                    Title = "درخواست ریموت دسکتاپ",
                    Message = $"{requesterName} درخواست دسترسی به سیستم شما را دارد.",
                    Type = NotificationType.RemoteRequest.ToString(),
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    RequesterId = requesterId.ToString(),
                    Data = $"{{\"serverIdentifier\":\"{serverIdentifier}\", \"requesterName\":\"{requesterName}\"}}"
                };

                // ارسال به سرور مشتری از طریق SignalR
                await _notificationHub.Clients.Group($"server_{serverIdentifier}")
                    .SendAsync("ReceiveRemoteRequest", notification);

                // ذخیره در دیتابیس
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"درخواست ریموت ارسال شد به سرور {serverIdentifier} از طرف {requesterName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در ارسال درخواست ریموت به سرور {serverIdentifier}");
            }
        }

        /// <summary>
        /// ارسال اعلان فعالیت سیستم
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="activity">نوع فعالیت</param>
        /// <param name="details">جزئیات فعالیت</param>
        /// <returns>Task</returns>
        public async Task SendActivityNotificationAsync(int userId, string activity, string details)
        {
            var title = $"فعالیت {activity}";
            var message = details;
            await SendToUserAsync(userId, title, message, NotificationType.Info);
        }

        /// <summary>
        /// ارسال اعلان هشدار امنیتی
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="securityIssue">مشکل امنیتی</param>
        /// <param name="details">جزئیات</param>
        /// <returns>Task</returns>
        public async Task SendSecurityAlertAsync(int userId, string securityIssue, string details)
        {
            var title = $"هشدار امنیتی: {securityIssue}";
            var message = details;
            await SendToUserAsync(userId, title, message, NotificationType.Warning);
        }

        /// <summary>
        /// ارسال اعلان اتمام انتقال فایل
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="fileName">نام فایل</param>
        /// <param name="isUpload">آپلود یا دانلود</param>
        /// <returns>Task</returns>
        public async Task SendFileTransferCompletedAsync(int userId, string fileName, bool isUpload)
        {
            var direction = isUpload ? "آپلود" : "دانلود";
            var title = $"{direction} فایل کامل شد";
            var message = $"فایل {fileName} با موفقیت {direction} شد.";
            await SendToUserAsync(userId, title, message, NotificationType.Success);
        }

        /// <summary>
        /// علامت‌گذاری اعلان به‌عنوان خوانده‌شده
        /// </summary>
        /// <param name="notificationId">شناسه اعلان</param>
        /// <returns>وضعیت بروزرسانی</returns>
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                    return false;

                notification.IsRead = true;
                notification.ReadTime = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در علامت‌گذاری اعلان {notificationId} به‌عنوان خوانده‌شده");
                return false;
            }
        }

        /// <summary>
        /// علامت‌گذاری همه اعلان‌های کاربر به‌عنوان خوانده‌شده
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <returns>تعداد اعلان‌های بروزرسانی‌شده</returns>
        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                if (!unreadNotifications.Any())
                    return 0;

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return unreadNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در علامت‌گذاری همه اعلان‌های کاربر {userId} به‌عنوان خوانده‌شده");
                return 0;
            }
        }

        /// <summary>
        /// دریافت اعلان‌های کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="onlyUnread">فقط خوانده‌نشده‌ها</param>
        /// <param name="count">تعداد</param>
        /// <returns>لیست اعلان‌ها</returns>
        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, bool onlyUnread = false, int count = 20)
        {
            try
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId || n.IsGlobal);

                if (onlyUnread)
                    query = query.Where(n => !n.IsRead);

                return await query
                    .OrderByDescending(n => n.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در دریافت اعلان‌های کاربر {userId}");
                return new List<Notification>();
            }
        }

        /// <summary>
        /// حذف اعلان
        /// </summary>
        /// <param name="notificationId">شناسه اعلان</param>
        /// <returns>وضعیت حذف</returns>
        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                    return false;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در حذف اعلان {notificationId}");
                return false;
            }
        }

        /// <summary>
        /// حذف همه اعلان‌های خوانده‌شده کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <returns>تعداد اعلان‌های حذف‌شده</returns>
        public async Task<int> DeleteReadNotificationsAsync(int userId)
        {
            try
            {
                var readNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead)
                    .ToListAsync();

                if (!readNotifications.Any())
                    return 0;

                _context.Notifications.RemoveRange(readNotifications);
                await _context.SaveChangesAsync();
                return readNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در حذف اعلان‌های خوانده‌شده کاربر {userId}");
                return 0;
            }
        }
    }

    /// <summary>
    /// انواع اعلان
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        RemoteRequest,
        FileTransfer,
        Chat,
        SecurityAlert,
        System
    }
}