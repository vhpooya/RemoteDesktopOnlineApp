using Microsoft.AspNetCore.SignalR;
using RemoteDesktopOnlineApps.Models;
using RemoteDesktopOnlineApps.Services;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RemoteDesktopOnlineApps.Hubs
{
    public class RemoteSessionHub : Hub
    {
        private readonly IRemoteDesktopService _remoteDesktopService;
        private readonly IRemoteDesktopStatsService _statsService;
        private readonly IClientIdentificationService _clientService;

        // مپ کردن شناسه کلاینت به شناسه اتصال SignalR
        private static readonly ConcurrentDictionary<string, string> _clientConnectionMap = new ConcurrentDictionary<string, string>();

        // مپ کردن شناسه اتصال SignalR به شناسه کلاینت
        private static readonly ConcurrentDictionary<string, string> _connectionClientMap = new ConcurrentDictionary<string, string>();

        // نگهداری ارتباط بین کاربران در حال کنترل از راه دور
        private static readonly ConcurrentDictionary<string, string> _remoteControlSessions = new ConcurrentDictionary<string, string>();

        public RemoteSessionHub(
            IRemoteDesktopService remoteDesktopService,
            IRemoteDesktopStatsService statsService,
            IClientIdentificationService clientService)
        {
            _remoteDesktopService = remoteDesktopService;
            _statsService = statsService;
            _clientService = clientService;
        }

        /// <summary>
        /// ثبت کلاینت در سرور
        /// </summary>
        public async Task RegisterClient(dynamic clientInfo)
        {
            try
            {
                // برای پیشگیری از خطاهای احتمالی، اطلاعات را استخراج می‌کنیم
                string clientId = clientInfo.ClientId.ToString();
                string accessKey = clientInfo.AccessKey.ToString();
                string machineName = clientInfo.MachineName?.ToString() ?? Environment.MachineName;
                string username = clientInfo.Username?.ToString() ?? "Unknown";
                string operatingSystem = clientInfo.OperatingSystem?.ToString() ?? "Unknown";

                // افزودن نگاشت کلاینت به اتصال و برعکس
                _clientConnectionMap[clientId] = Context.ConnectionId;
                _connectionClientMap[Context.ConnectionId] = clientId;

                // پیوستن به گروه منحصر به فرد این کلاینت
                await Groups.AddToGroupAsync(Context.ConnectionId, $"client_{clientId}");

                // ثبت یا به‌روزرسانی اطلاعات در دیتابیس
                var clientData = new
                {
                    ClientId = clientId,
                    AccessKey = accessKey,
                    MachineName = machineName,
                    Username = username,
                    OperatingSystem = operatingSystem,
                    ConnectionId = Context.ConnectionId,
                    IsOnline = true,
                    LastSeen = DateTime.Now
                };

                // بررسی صحت اطلاعات و ثبت در بانک اطلاعاتی
                bool registered = await _clientService.UpdateClientStatusAsync(clientId, true);

                // ارسال تأییدیه ثبت
                await Clients.Caller.SendAsync("RegistrationConfirmed", registered, DateTime.Now);

                // اعلام به مدیران
                await Clients.Group("Administrators").SendAsync("ClientConnected", clientData);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", $"خطا در ثبت کلاینت: {ex.Message}");
            }
        }

        /// <summary>
        /// پیوستن به گروه مخصوص یک کلاینت
        /// </summary>
        public async Task JoinClientGroup(string clientId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"client_{clientId}");
        }

        /// <summary>
        /// لغو ثبت کلاینت
        /// </summary>
        public async Task UnregisterClient(string clientId)
        {
            // برداشتن از گروه
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"client_{clientId}");

            // حذف از نگاشت‌ها
            _clientConnectionMap.TryRemove(clientId, out _);
            _connectionClientMap.TryRemove(Context.ConnectionId, out _);

            // به‌روزرسانی وضعیت در دیتابیس
            await _clientService.UpdateClientStatusAsync(clientId, false);

            // اعلام به مدیران
            await Clients.Group("Administrators").SendAsync("ClientDisconnected", clientId);
        }

        /// <summary>
        /// درخواست کنترل از راه دور
        /// </summary>
        public async Task RequestRemoteControl(string connectionId, string clientId, string accessKey)
        {
            try
            {
                // بررسی وجود کلاینت مورد نظر
                if (!_clientConnectionMap.TryGetValue(clientId, out string targetConnectionId))
                {
                    await Clients.Caller.SendAsync("RemoteControlRejected", clientId, "کلاینت مورد نظر آنلاین نیست");
                    return;
                }

                // بررسی اعتبار کلید دسترسی
                bool isValid = await _clientService.ValidateClientAsync(clientId, accessKey);
                if (!isValid)
                {
                    await Clients.Caller.SendAsync("RemoteControlRejected", clientId, "کلید دسترسی نامعتبر است");
                    return;
                }

                // نام کاربر درخواست‌کننده (در نسخه واقعی از Context.User استخراج می‌شود)
                string supportUserName = "کاربر پشتیبانی";
                string supportUserId = connectionId;

                // ارسال درخواست به کلاینت مقصد
                await Clients.Client(targetConnectionId).SendAsync("RemoteControlRequest", supportUserId, supportUserName, connectionId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", $"خطا در درخواست کنترل از راه دور: {ex.Message}");
            }
        }

        /// <summary>
        /// پذیرش درخواست کنترل از راه دور
        /// </summary>
        public async Task AcceptRemoteControl(string clientId, string supportConnectionId)
        {
            try
            {
                // ذخیره ارتباط کنترل از راه دور
                _remoteControlSessions[clientId] = supportConnectionId;
                _remoteControlSessions[supportConnectionId] = clientId;

                // ارسال تأییدیه به کاربر پشتیبانی
                await Clients.Client(supportConnectionId).SendAsync("RemoteControlAccepted", clientId, Context.ConnectionId);

                // ارسال پیام تأیید به کلاینت
                await Clients.Caller.SendAsync("ReceiveMessage", "کنترل از راه دور فعال شد");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", $"خطا در پذیرش درخواست: {ex.Message}");
            }
        }

        /// <summary>
        /// رد درخواست کنترل از راه دور
        /// </summary>
        public async Task RejectRemoteControl(string clientId, string supportConnectionId)
        {
            try
            {
                // ارسال پیام رد درخواست به کاربر پشتیبانی
                await Clients.Client(supportConnectionId).SendAsync("RemoteControlRejected", clientId, "درخواست توسط کاربر رد شد");

                // ارسال پیام تأیید به کلاینت
                await Clients.Caller.SendAsync("ReceiveMessage", "درخواست کنترل از راه دور رد شد");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", $"خطا در رد درخواست: {ex.Message}");
            }
        }

        /// <summary>
        /// توقف کنترل توسط کاربر پشتیبانی
        /// </summary>
        public async Task StopControllingClient(string clientId)
        {
            try
            {
                // برداشتن از لیست جلسات کنترل از راه دور
                _remoteControlSessions.TryRemove(Context.ConnectionId, out _);

                // در صورتی که کلاینت هنوز آنلاین است
                if (_clientConnectionMap.TryGetValue(clientId, out string targetConnectionId))
                {
                    // اعلام به کلاینت
                    await Clients.Client(targetConnectionId).SendAsync("SupportStoppedControlling", Context.ConnectionId);

                    // حذف کلاینت از لیست جلسات
                    _remoteControlSessions.TryRemove(clientId, out _);
                }

                // ارسال پیام تأیید به کاربر پشتیبانی
                await Clients.Caller.SendAsync("ReceiveMessage", "کنترل از راه دور متوقف شد");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", $"خطا در توقف کنترل: {ex.Message}");
            }
        }

        /// <summary>
        /// توقف کنترل توسط کلاینت
        /// </summary>
        public async Task StopBeingControlled(string clientId, string supportConnectionId)
        {
            try
            {
                // برداشتن از لیست جلسات کنترل از راه دور
                _remoteControlSessions.TryRemove(clientId, out _);
                _remoteControlSessions.TryRemove(supportConnectionId, out _);

                // اعلام به کاربر پشتیبانی
                await Clients.Client(supportConnectionId).SendAsync("ClientStoppedBeingControlled", clientId);

                // ارسال پیام تأیید به کلاینت
                await Clients.Caller.SendAsync("ReceiveMessage", "کنترل از راه دور متوقف شد");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", $"خطا در توقف کنترل: {ex.Message}");
            }
        }

        /// <summary>
        /// ارسال داده صفحه نمایش
        /// </summary>
        public async Task SendScreenData(string clientId, string targetConnectionId, byte[] screenData)
        {
            // بررسی مجوز ارسال (فقط اگر کلاینت در حال کنترل شدن توسط همین کاربر پشتیبانی باشد)
            if (_remoteControlSessions.TryGetValue(clientId, out string supportConnectionId) &&
                supportConnectionId == targetConnectionId)
            {
                await Clients.Client(targetConnectionId).SendAsync("ReceiveScreenData", screenData);
            }
        }

        /// <summary>
        /// ارسال رویداد ماوس
        /// </summary>
        public async Task SendMouseEvent(string targetClientId, int x, int y, string eventType, int button = 0)
        {
            // بررسی مجوز ارسال (فقط اگر کاربر پشتیبانی در حال کنترل این کلاینت باشد)
            if (_remoteControlSessions.TryGetValue(Context.ConnectionId, out string clientId) &&
                clientId == targetClientId)
            {
                if (_clientConnectionMap.TryGetValue(targetClientId, out string targetConnectionId))
                {
                    await Clients.Client(targetConnectionId).SendAsync("ReceiveMouseEvent", x, y, eventType, button);
                }
            }
        }

        /// <summary>
        /// ارسال رویداد کیبورد
        /// </summary>
        public async Task SendKeyboardEvent(string targetClientId, string keyCode, bool isKeyDown)
        {
            // بررسی مجوز ارسال (فقط اگر کاربر پشتیبانی در حال کنترل این کلاینت باشد)
            if (_remoteControlSessions.TryGetValue(Context.ConnectionId, out string clientId) &&
                clientId == targetClientId)
            {
                if (_clientConnectionMap.TryGetValue(targetClientId, out string targetConnectionId))
                {
                    await Clients.Client(targetConnectionId).SendAsync("ReceiveKeyboardEvent", keyCode, isKeyDown);
                }
            }
        }

        /// <summary>
        /// رویداد اتصال جدید
        /// </summary>
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// رویداد قطع اتصال
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // بررسی آیا این اتصال مربوط به کلاینت ثبت شده است
            if (_connectionClientMap.TryRemove(Context.ConnectionId, out string clientId))
            {
                // حذف از نگاشت کلاینت به اتصال
                _clientConnectionMap.TryRemove(clientId, out _);

                // به‌روزرسانی وضعیت در دیتابیس
                await _clientService.UpdateClientStatusAsync(clientId, false);

                // اعلام به مدیران
                await Clients.Group("Administrators").SendAsync("ClientDisconnected", clientId);

                // بررسی اگر این کلاینت در حال کنترل شدن بود
                if (_remoteControlSessions.TryRemove(clientId, out string supportConnectionId))
                {
                    // اعلام به کاربر پشتیبانی
                    await Clients.Client(supportConnectionId).SendAsync("ClientStoppedBeingControlled", clientId);

                    // حذف از جلسات کنترل از راه دور
                    _remoteControlSessions.TryRemove(supportConnectionId, out _);
                }
            }
            else
            {
                // بررسی اگر این اتصال مربوط به کاربر پشتیبانی بود که در حال کنترل کلاینت بود
                if (_remoteControlSessions.TryRemove(Context.ConnectionId, out string controlledClientId))
                {
                    // اعلام به کلاینت
                    if (_clientConnectionMap.TryGetValue(controlledClientId, out string clientConnectionId))
                    {
                        await Clients.Client(clientConnectionId).SendAsync("SupportStoppedControlling", Context.ConnectionId);
                    }

                    // حذف از جلسات کنترل از راه دور
                    _remoteControlSessions.TryRemove(controlledClientId, out _);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

}