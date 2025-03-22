using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteDesktopClient.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RemoteDesktopClient.Services
{
    /// <summary>
    /// سرویس اتصال به سرور SignalR و مدیریت ارتباطات
    /// </summary>
    public class ConnectionService : IDisposable
    {
        #region متغیرها و ویژگی‌ها

        // اتصال SignalR
        private HubConnection _hubConnection;
        private readonly HttpClient _httpClient;

        // اطلاعات اتصال
        public ConnectionInfo ConnectionInfo { get; private set; }

        // وضعیت کنترل از راه دور
        public bool IsControlling { get; private set; }
        public bool IsBeingControlled { get; private set; }

        // مسیر فایل پیکربندی
        private readonly string _configFilePath;

        // رویدادها
        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<string> MessageReceived;
        public event EventHandler<byte[]> ScreenDataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<RemoteControlRequestEventArgs> RemoteControlRequestReceived;
        public event EventHandler<RemoteControlStatusEventArgs> RemoteControlStatusChanged;
        public event EventHandler<RemoteMouseEventArgs> MouseEventReceived;
        public event EventHandler<KeyboardEventArgs> KeyboardEventReceived;

        #endregion

        /// <summary>
        /// سازنده کلاس ConnectionService
        /// </summary>
        public ConnectionService()
        {
            // مقداردهی اولیه
            ConnectionInfo = new ConnectionInfo();
            _httpClient = new HttpClient();
            IsControlling = false;
            IsBeingControlled = false;

            // تنظیم مسیر فایل پیکربندی
            _configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RemoteDesktopClient2",
                "config.json");

            // ایجاد پوشه در صورت نیاز
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));

            // بارگذاری یا ایجاد شناسه کلاینت
            LoadOrCreateClientIdentity();

            // ثبت رویداد خروج برنامه
            Application.Current.Exit += (s, e) => Dispose();
        }

        #region متدهای اصلی

        /// <summary>
        /// ایجاد و راه‌اندازی اتصال SignalR
        /// </summary>
        public async Task InitializeConnectionAsync(string serverUrl)
        {
            try
            {
                // اگر قبلاً اتصال برقرار شده، ابتدا آن را قطع می‌کنیم
                if (_hubConnection != null)
                {
                    await DisconnectAsync();
                }

                // ذخیره آدرس سرور
                ConnectionInfo.ServerUrl = serverUrl;

                // ساخت اتصال SignalR
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{serverUrl.TrimEnd('/')}/remoteSessionHub", options =>
                    {
                        // امکان ارسال فایل‌های بزرگ
                        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                        options.WebSocketConfiguration = conf => conf.KeepAliveInterval = TimeSpan.FromSeconds(30);
                    })
                    .AddJsonProtocol()
                    .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                   .ConfigureLogging(logging =>
                   {
                       logging.SetMinimumLevel(LogLevel.Debug);
                       // حذف خط AddDebug
                   })
                    .Build();

                // ثبت رویدادهای SignalR
                RegisterHubEvents();

                // شروع اتصال به سرور
                LogMessage("در حال اتصال به سرور...");
                await _hubConnection.StartAsync();
                LogMessage($"اتصال به سرور {serverUrl} برقرار شد");

                // ثبت کلاینت در سرور
                await RegisterClientWithServer();

                // به‌روزرسانی وضعیت اتصال
                ConnectionInfo.IsConnected = true;
                OnConnectionStatusChanged(true);
            }
            catch (Exception ex)
            {
                string errorMessage = $"خطا در برقراری اتصال به سرور: {ex.Message}";
                Debug.WriteLine(errorMessage);
                Debug.WriteLine(ex.StackTrace);

                OnErrorOccurred(errorMessage);
                ConnectionInfo.IsConnected = false;
                OnConnectionStatusChanged(false);

                throw;
            }
        }

        /// <summary>
        /// ثبت کلاینت در سرور SignalR
        /// </summary>
        private async Task RegisterClientWithServer()
        {
            try
            {
                // اطلاعات سیستم برای ارسال به سرور
                var systemInfo = new
                {
                    ClientId = ConnectionInfo.ClientId,
                    AccessKey = ConnectionInfo.AccessKey,
                    MachineName = Environment.MachineName,
                    Username = Environment.UserName,
                    OperatingSystem = Environment.OSVersion.ToString(),
                    ScreenWidth = SystemParameters.PrimaryScreenWidth,
                    ScreenHeight = SystemParameters.PrimaryScreenHeight,
                    ConnectionTime = DateTime.Now
                };

                // ثبت کلاینت در سرور
                await _hubConnection.InvokeAsync("RegisterClient", systemInfo);

                // پیوستن به گروه کلاینت
                await _hubConnection.InvokeAsync("JoinClientGroup", ConnectionInfo.ClientId);

                LogMessage($"کلاینت با شناسه {ConnectionInfo.ClientId} در سرور ثبت شد");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در ثبت کلاینت در سرور: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// قطع اتصال از سرور
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                // قطع کنترل از راه دور در صورت فعال بودن
                if (IsControlling || IsBeingControlled)
                {
                    await StopRemoteControlAsync();
                }

                // اعلام آفلاین شدن به سرور
                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("UnregisterClient", ConnectionInfo.ClientId);
                    LogMessage("کلاینت از سرور خارج شد");
                }

                // قطع اتصال SignalR
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                // به‌روزرسانی وضعیت
                ConnectionInfo.IsConnected = false;
                OnConnectionStatusChanged(false);

                LogMessage("اتصال از سرور قطع شد");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در قطع اتصال: {ex.Message}");
            }
        }

        /// <summary>
        /// درخواست کنترل از راه دور
        /// </summary>
        public async Task RequestRemoteControlAsync(string targetClientId, string accessKey)
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    OnErrorOccurred("اتصال به سرور برقرار نیست");
                    return;
                }

                LogMessage($"ارسال درخواست کنترل از راه دور به کلاینت {targetClientId}...");

                // ارسال درخواست کنترل از راه دور
                await _hubConnection.InvokeAsync("RequestRemoteControl", _hubConnection.ConnectionId, targetClientId, accessKey);

                LogMessage($"درخواست کنترل از راه دور به کلاینت {targetClientId} ارسال شد");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در ارسال درخواست کنترل از راه دور: {ex.Message}");
            }
        }

        /// <summary>
        /// پذیرش درخواست کنترل از راه دور
        /// </summary>
        public async Task AcceptRemoteControlRequestAsync(string supportUserConnectionId)
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    OnErrorOccurred("اتصال به سرور برقرار نیست");
                    return;
                }

                // ذخیره اطلاعات کنترل‌کننده
                ConnectionInfo.RemoteSupportConnectionId = supportUserConnectionId;

                // ارسال پاسخ پذیرش
                await _hubConnection.InvokeAsync("AcceptRemoteControl", ConnectionInfo.ClientId, supportUserConnectionId);

                // به‌روزرسانی وضعیت
                IsBeingControlled = true;

                // اعلام تغییر وضعیت
                OnRemoteControlStatusChanged(new RemoteControlStatusEventArgs
                {
                    IsControlling = false,
                    IsBeingControlled = true,
                    RemoteUserConnectionId = supportUserConnectionId
                });

                LogMessage("درخواست کنترل از راه دور پذیرفته شد");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در پذیرش درخواست کنترل از راه دور: {ex.Message}");
            }
        }

        /// <summary>
        /// رد درخواست کنترل از راه دور
        /// </summary>
        public async Task RejectRemoteControlRequestAsync(string supportUserConnectionId)
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    OnErrorOccurred("اتصال به سرور برقرار نیست");
                    return;
                }

                // ارسال پاسخ رد
                await _hubConnection.InvokeAsync("RejectRemoteControl", ConnectionInfo.ClientId, supportUserConnectionId);

                LogMessage("درخواست کنترل از راه دور رد شد");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در رد درخواست کنترل از راه دور: {ex.Message}");
            }
        }

        /// <summary>
        /// توقف کنترل از راه دور
        /// </summary>
        public async Task StopRemoteControlAsync()
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    OnErrorOccurred("اتصال به سرور برقرار نیست");
                    return;
                }

                if (IsControlling)
                {
                    // اگر در حال کنترل هستیم، اطلاع‌رسانی به کلاینت مقابل
                    await _hubConnection.InvokeAsync("StopControllingClient", ConnectionInfo.RemoteClientId);
                    LogMessage($"کنترل کلاینت {ConnectionInfo.RemoteClientId} متوقف شد");
                }
                else if (IsBeingControlled)
                {
                    // اگر در حال کنترل شدن هستیم، اطلاع‌رسانی به کاربر پشتیبانی
                    await _hubConnection.InvokeAsync("StopBeingControlled", ConnectionInfo.ClientId, ConnectionInfo.RemoteSupportConnectionId);
                    LogMessage("کنترل شدن توسط کاربر پشتیبانی متوقف شد");
                }

                // بازنشانی وضعیت
                IsControlling = false;
                IsBeingControlled = false;
                ConnectionInfo.RemoteClientId = null;
                ConnectionInfo.RemoteSupportConnectionId = null;

                // اعلام تغییر وضعیت
                OnRemoteControlStatusChanged(new RemoteControlStatusEventArgs
                {
                    IsControlling = false,
                    IsBeingControlled = false
                });

                LogMessage("کنترل از راه دور متوقف شد");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در توقف کنترل از راه دور: {ex.Message}");
            }
        }

        /// <summary>
        /// ارسال داده تصویر صفحه نمایش
        /// </summary>
        public async Task SendScreenDataAsync(byte[] screenData)
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected || !IsBeingControlled)
                {
                    return; // بدون اعلام خطا برای جلوگیری از پیام‌های مکرر
                }

                // ارسال داده تصویر به کاربر پشتیبانی
                await _hubConnection.InvokeAsync("SendScreenData",
                    ConnectionInfo.ClientId,
                    ConnectionInfo.RemoteSupportConnectionId,
                    screenData);

                // لاگ کردن در حالت دیباگ
                Debug.WriteLine($"ارسال تصویر صفحه: {screenData.Length} بایت");
            }
            catch (Exception ex)
            {
                // فقط در حالت دیباگ خطا را ثبت می‌کنیم
                Debug.WriteLine($"خطا در ارسال داده تصویر: {ex.Message}");
            }
        }

        /// <summary>
        /// ارسال رویداد ماوس
        /// </summary>
        public async Task SendMouseEventAsync(int x, int y, string eventType, int button = 0)
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected || !IsControlling)
                {
                    return;
                }

                // ارسال رویداد ماوس به کلاینت تحت کنترل
                await _hubConnection.InvokeAsync("SendMouseEvent",
                    ConnectionInfo.RemoteClientId,
                    x, y, eventType, button);

                // لاگ کردن در حالت دیباگ
                Debug.WriteLine($"ارسال رویداد ماوس: {eventType} در موقعیت {x},{y}, دکمه {button}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد ماوس: {ex.Message}");
            }
        }

        /// <summary>
        /// ارسال رویداد کیبورد
        /// </summary>
        public async Task SendKeyboardEventAsync(string keyCode, bool isKeyDown)
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected || !IsControlling)
                {
                    return;
                }

                // ارسال رویداد کیبورد به کلاینت تحت کنترل
                await _hubConnection.InvokeAsync("SendKeyboardEvent",
                    ConnectionInfo.RemoteClientId,
                    keyCode, isKeyDown);

                // لاگ کردن در حالت دیباگ
                Debug.WriteLine($"ارسال رویداد کیبورد: کلید {keyCode}, {(isKeyDown ? "فشرده" : "رها")} شد");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد کیبورد: {ex.Message}");
            }
        }

        /// <summary>
        /// دریافت وضعیت اتصال
        /// </summary>
        public string GetConnectionStatus()
        {
            if (_hubConnection == null)
                return "عدم اتصال";

            switch (_hubConnection.State)
            {
                case HubConnectionState.Connected:
                    return "متصل";
                case HubConnectionState.Connecting:
                    return "در حال اتصال";
                case HubConnectionState.Disconnected:
                    return "قطع";
                case HubConnectionState.Reconnecting:
                    return "در حال اتصال مجدد";
                default:
                    return "نامشخص";
            }
        }

        #endregion

        #region متدهای کمکی و رویدادها

        /// <summary>
        /// بارگذاری یا ایجاد اطلاعات هویتی کلاینت
        /// </summary>
        private void LoadOrCreateClientIdentity()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var clientIdentity = System.Text.Json.JsonSerializer.Deserialize<ClientIdentity>(json);

                    if (clientIdentity != null && !string.IsNullOrEmpty(clientIdentity.ClientId) && !string.IsNullOrEmpty(clientIdentity.AccessKey))
                    {
                        ConnectionInfo.ClientId = clientIdentity.ClientId;
                        ConnectionInfo.AccessKey = clientIdentity.AccessKey;
                        LogMessage($"اطلاعات کلاینت از فایل پیکربندی بارگذاری شد (شناسه: {clientIdentity.ClientId})");
                        return;
                    }
                }

                // ایجاد شناسه و کلید دسترسی جدید
                ConnectionInfo.ClientId = GenerateClientId();
                ConnectionInfo.AccessKey = GenerateAccessKey();

                // ذخیره اطلاعات
                SaveClientIdentity();

                LogMessage($"اطلاعات کلاینت جدید ایجاد شد (شناسه: {ConnectionInfo.ClientId})");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در بارگذاری اطلاعات هویتی: {ex.Message}");

                // ایجاد اطلاعات جدید در صورت بروز خطا
                ConnectionInfo.ClientId = GenerateClientId();
                ConnectionInfo.AccessKey = GenerateAccessKey();
                SaveClientIdentity();
            }
        }

        /// <summary>
        /// ذخیره اطلاعات هویتی کلاینت
        /// </summary>
        private void SaveClientIdentity()
        {
            try
            {
                var clientIdentity = new ClientIdentity
                {
                    ClientId = ConnectionInfo.ClientId,
                    AccessKey = ConnectionInfo.AccessKey,
                    MachineName = Environment.MachineName,
                    OperatingSystem = Environment.OSVersion.ToString(),
                    CreatedDate = DateTime.Now
                };

                string json = System.Text.Json.JsonSerializer.Serialize(clientIdentity, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_configFilePath, json);

                LogMessage("اطلاعات کلاینت ذخیره شد");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطا در ذخیره اطلاعات هویتی: {ex.Message}");
            }
        }

        /// <summary>
        /// تولید شناسه یکتا برای کلاینت
        /// </summary>
        private string GenerateClientId()
        {
            try
            {
                string macAddress = GetMacAddress();

                using (var md5 = MD5.Create())
                {
                    string input = macAddress + Environment.MachineName;
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

                    var id = BitConverter.ToUInt64(hashBytes, 0) % 1000000000;
                    return "2" + id.ToString("000000000").Substring(1);
                }
            }
            catch
            {
                Random random = new Random();
                return random.Next(100000000, 999999999).ToString();
            }
        }

        /// <summary>
        /// تولید کلید دسترسی
        /// </summary>
        private string GenerateAccessKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);

                int value = BitConverter.ToInt32(randomBytes, 0);
                value = Math.Abs(value % 1000000);

                return value.ToString("000000");
            }
        }

        /// <summary>
        /// دریافت آدرس MAC کارت شبکه
        /// </summary>
        private string GetMacAddress()
        {
            return (from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up &&
                          nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                    select nic.GetPhysicalAddress().ToString()
                   ).FirstOrDefault() ?? "NOMACADDRESS";
        }

        /// <summary>
        /// ثبت رویدادهای هاب SignalR
        /// </summary>
        private void RegisterHubEvents()
        {
            // درخواست کنترل از راه دور
            _hubConnection.On<string, string, string>("RemoteControlRequest",
                (supportUserId, supportUserName, supportConnectionId) =>
                {
                    LogMessage($"درخواست کنترل از راه دور از {supportUserName} دریافت شد");

                    OnRemoteControlRequestReceived(new RemoteControlRequestEventArgs
                    {
                        SupportUserId = supportUserId,
                        SupportUserName = supportUserName,
                        ConnectionId = supportConnectionId
                    });
                });

            // پذیرش درخواست کنترل
            _hubConnection.On<string, string>("RemoteControlAccepted",
                (clientId, clientName) =>
                {
                    LogMessage($"درخواست کنترل از راه دور توسط {clientName} پذیرفته شد");

                    // به‌روزرسانی وضعیت
                    IsControlling = true;
                    ConnectionInfo.RemoteClientId = clientId;

                    OnRemoteControlStatusChanged(new RemoteControlStatusEventArgs
                    {
                        IsControlling = true,
                        IsBeingControlled = false,
                        RemoteClientId = clientId,
                        RemoteClientName = clientName
                    });
                });

            // رد درخواست کنترل
            _hubConnection.On<string, string>("RemoteControlRejected",
                (clientId, reason) =>
                {
                    LogMessage($"درخواست کنترل از راه دور توسط {clientId} رد شد: {reason}");
                });

            // دریافت داده صفحه نمایش
            _hubConnection.On<byte[]>("ReceiveScreenData",
                (screenData) =>
                {
                    // اعلام رویداد دریافت داده صفحه
                    ScreenDataReceived?.Invoke(this, screenData);
                });

            // دریافت رویداد ماوس
            _hubConnection.On<int, int, string, int>("ReceiveMouseEvent",
                (x, y, eventType, button) =>
                {
                    // اعلام رویداد دریافت رویداد ماوس
                    MouseEventReceived?.Invoke(this, new RemoteMouseEventArgs
                    {
                        X = x,
                        Y = y,
                        EventType = eventType,
                        Button = button
                    });
                });

            // دریافت رویداد کیبورد
            _hubConnection.On<string, bool>("ReceiveKeyboardEvent",
                (keyCode, isKeyDown) =>
                {
                    // اعلام رویداد دریافت رویداد کیبورد
                    KeyboardEventReceived?.Invoke(this, new KeyboardEventArgs
                    {
                        KeyCode = keyCode,
                        IsKeyDown = isKeyDown
                    });
                });

            // قطع کنترل توسط کلاینت
            _hubConnection.On<string>("ClientStoppedBeingControlled",
                (clientId) =>
                {
                    LogMessage($"کنترل از راه دور توسط کلاینت {clientId} قطع شد");

                    // بازنشانی وضعیت
                    IsControlling = false;
                    ConnectionInfo.RemoteClientId = null;

                    OnRemoteControlStatusChanged(new RemoteControlStatusEventArgs
                    {
                        IsControlling = false,
                        IsBeingControlled = false
                    });
                });

            // قطع کنترل توسط کاربر پشتیبانی
            _hubConnection.On<string>("SupportStoppedControlling",
                (supportConnectionId) =>
                {
                    LogMessage("کنترل از راه دور توسط کاربر پشتیبانی قطع شد");

                    // بازنشانی وضعیت
                    IsBeingControlled = false;
                    ConnectionInfo.RemoteSupportConnectionId = null;

                    OnRemoteControlStatusChanged(new RemoteControlStatusEventArgs
                    {
                        IsControlling = false,
                        IsBeingControlled = false
                    });
                });

            // دریافت پیام عمومی
            _hubConnection.On<string>("ReceiveMessage",
                (message) =>
                {
                    MessageReceived?.Invoke(this, message);
                });

            // دریافت خطا
            _hubConnection.On<string>("ReceiveError",
                (error) =>
                {
                    OnErrorOccurred(error);
                });

            // رویدادهای اتصال مجدد
            _hubConnection.Reconnecting += error =>
            {
                LogMessage($"اتصال قطع شده است. در حال تلاش برای اتصال مجدد... {error?.Message ?? ""}");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                LogMessage($"اتصال مجدد برقرار شد. شناسه اتصال: {connectionId}");

                // ثبت مجدد کلاینت
                RegisterClientWithServer().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        OnErrorOccurred($"خطا در ثبت مجدد کلاینت پس از اتصال مجدد: {task.Exception.InnerException?.Message}");
                    }
                });

                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                LogMessage($"اتصال بسته شد. {error?.Message ?? ""}");

                // به‌روزرسانی وضعیت
                ConnectionInfo.IsConnected = false;
                OnConnectionStatusChanged(false);

                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// ثبت پیام در لاگ و ارسال به شنوندگان
        /// </summary>
        private void LogMessage(string message)
        {
            Debug.WriteLine($"[ConnectionService] {message}");
            MessageReceived?.Invoke(this, message);
        }

        /// <summary>
        /// اعلام تغییر وضعیت اتصال
        /// </summary>
        private void OnConnectionStatusChanged(bool isConnected)
        {
            ConnectionStatusChanged?.Invoke(this, isConnected);
        }

        /// <summary>
        /// اعلام خطا
        /// </summary>
        private void OnErrorOccurred(string errorMessage)
        {
            Debug.WriteLine($"[ConnectionService] Error: {errorMessage}");
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        /// <summary>
        /// اعلام درخواست کنترل از راه دور
        /// </summary>
        private void OnRemoteControlRequestReceived(RemoteControlRequestEventArgs args)
        {
            RemoteControlRequestReceived?.Invoke(this, args);
        }

        /// <summary>
        /// اعلام تغییر وضعیت کنترل از راه دور
        /// </summary>
        private void OnRemoteControlStatusChanged(RemoteControlStatusEventArgs args)
        {
            RemoteControlStatusChanged?.Invoke(this, args);

            // ذخیره اطلاعات ارتباط در ConnectionInfo
            if (args.IsControlling)
            {
                ConnectionInfo.RemoteClientId = args.RemoteClientId;
            }
            else if (args.IsBeingControlled)
            {
                ConnectionInfo.RemoteSupportConnectionId = args.RemoteUserConnectionId;
            }
            else
            {
                ConnectionInfo.RemoteClientId = null;
                ConnectionInfo.RemoteSupportConnectionId = null;
            }
        }

        #endregion

        /// <summary>
        /// آزادسازی منابع
        /// </summary>
        public void Dispose()
        {
            try
            {
                // قطع اتصال SignalR
                if (_hubConnection != null)
                {
                    _hubConnection.StopAsync().Wait();
                    _hubConnection.DisposeAsync().AsTask().Wait();
                }

                // آزادسازی منابع HTTP
                _httpClient.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در آزادسازی منابع: {ex.Message}");
            }
        }
    }

    #region کلاس‌های کمکی

    /// <summary>
    /// اطلاعات هویتی کلاینت
    /// </summary>
    public class ClientIdentity
    {
        public string ClientId { get; set; }
        public string AccessKey { get; set; }
        public string MachineName { get; set; }
        public string OperatingSystem { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// پارامترهای درخواست کنترل از راه دور
    /// </summary>
    public class RemoteControlRequestEventArgs : EventArgs
    {
        public string SupportUserId { get; set; }
        public string SupportUserName { get; set; }
        public string ConnectionId { get; set; }
    }

    /// <summary>
    /// پارامترهای تغییر وضعیت کنترل از راه دور
    /// </summary>
    public class RemoteControlStatusEventArgs : EventArgs
    {
        public bool IsControlling { get; set; }
        public bool IsBeingControlled { get; set; }
        public string RemoteClientId { get; set; }
        public string RemoteClientName { get; set; }
        public string RemoteUserConnectionId { get; set; }
    }

    /// <summary>
    /// پارامترهای رویداد ماوس
    /// </summary>
    public class RemoteMouseEventArgs : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string EventType { get; set; }
        public int Button { get; set; }
    }

    /// <summary>
    /// پارامترهای رویداد کیبورد
    /// </summary>
    public class KeyboardEventArgs : EventArgs
    {
        public string KeyCode { get; set; }
        public bool IsKeyDown { get; set; }
    }

    #endregion
}