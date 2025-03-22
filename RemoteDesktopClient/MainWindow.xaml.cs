using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RemoteDesktopClient.Models;
using RemoteDesktopClient.Services;
using RemoteDesktopClient.Views;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Linq;

namespace RemoteDesktopClient
{
    /// <summary>
    /// کلاس منطقی پنجره اصلی برنامه
    /// </summary>
    public partial class MainWindow : Window
    {
        // سرویس‌های اصلی برنامه
        private readonly ConnectionService _connectionService;
        private readonly ScreenCaptureService _screenCaptureService;
        private readonly InputService _inputService;

        // تایمر برای نمایش FPS
        private readonly DispatcherTimer _fpsTimer;
        private readonly DispatcherTimer _statusTimer;
        private int _frameCount;
        private DateTime _lastFpsUpdate;

        // پنجره درخواست کنترل از راه دور
        private RemoteControlRequestWindow _remoteControlRequestWindow;

        // مسیر ذخیره تنظیمات
        private readonly string _settingsPath;

        // ایجاد شیء اصلی
        public MainWindow()
        {
            InitializeComponent();

            // تنظیم مسیر ذخیره تنظیمات
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RemoteDesktopClient",
                "settings.json");

            // مقداردهی سرویس‌ها
            _connectionService = new ConnectionService();
            _screenCaptureService = new ScreenCaptureService();
            _inputService = new InputService();

            // ثبت رویدادها
            RegisterEventHandlers();

            // راه‌اندازی تایمر FPS
            _fpsTimer = new DispatcherTimer();
            _fpsTimer.Interval = TimeSpan.FromSeconds(1);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            // راه‌اندازی تایمر به‌روزرسانی وضعیت
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(5);
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();

            _lastFpsUpdate = DateTime.Now;
            _frameCount = 0;

            // تنظیم عنوان برنامه
            this.Title = $"سیستم پشتیبانی از راه دور - نسخه 1.0";
        }

        /// <summary>
        /// رویداد بارگذاری پنجره
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // نمایش اطلاعات کلاینت
                DisplayClientInfo();

                // بارگذاری تنظیمات
                LoadSettings();

                // افزودن پیام خوش‌آمدگویی
                AddSystemMessage("به سیستم پشتیبانی از راه دور خوش آمدید.");
                AddSystemMessage("شناسه و رمز ورود شما در بالای صفحه نمایش داده شده است.");
                AddSystemMessage("برای شروع، به سرور متصل شوید.");

                // اتصال خودکار در صورت تنظیم
                if (_connectionService.ConnectionInfo.Settings.AutoConnect &&
                    !string.IsNullOrEmpty(_connectionService.ConnectionInfo.ServerUrl))
                {
                    ConnectToServer(_connectionService.ConnectionInfo.ServerUrl);
                }
            }
            catch (Exception ex)
            {
                OnError($"خطا در بارگذاری برنامه: {ex.Message}");
            }
        }

        /// <summary>
        /// ثبت رویدادها
        /// </summary>
        private void RegisterEventHandlers()
        {
            // رویدادهای سرویس اتصال
            _connectionService.ConnectionStatusChanged += ConnectionService_ConnectionStatusChanged;
            _connectionService.MessageReceived += ConnectionService_MessageReceived;
            _connectionService.ScreenDataReceived += ConnectionService_ScreenDataReceived;
            _connectionService.ErrorOccurred += ConnectionService_ErrorOccurred;
            _connectionService.RemoteControlRequestReceived += ConnectionService_RemoteControlRequestReceived;
            _connectionService.RemoteControlStatusChanged += ConnectionService_RemoteControlStatusChanged;

            // رویدادهای سرویس ضبط صفحه
            _screenCaptureService.ScreenCaptured += ScreenCaptureService_ScreenCaptured;
            _screenCaptureService.CaptureError += ScreenCaptureService_CaptureError;

            // رویدادهای سرویس ورودی
            _inputService.InputProcessed += InputService_InputProcessed;

            // رویدادهای کلیدهای میانبر
            this.KeyDown += MainWindow_KeyDown;
        }

        /// <summary>
        /// نمایش اطلاعات کلاینت
        /// </summary>
        private void DisplayClientInfo()
        {
            txtMyId.Text = _connectionService.ConnectionInfo.ClientId;
            txtMyPassword.Password = _connectionService.ConnectionInfo.AccessKey;
            txtMyComputerName.Text = _connectionService.ConnectionInfo.MachineName;
        }

        /// <summary>
        /// بارگذاری تنظیمات
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                    {
                        // اعمال تنظیمات سرور
                        if (!string.IsNullOrEmpty(settings.ServerUrl))
                        {
                            txtServerUrl.Text = settings.ServerUrl;
                        }

                        // اعمال تنظیمات کیفیت تصویر
                        if (settings.ImageQuality > 0)
                        {
                            sldQuality.Value = settings.ImageQuality;
                        }

                        // اعمال تنظیمات اتصال
                        if (settings.ConnectionSettings != null)
                        {
                            _connectionService.ConnectionInfo.Settings = settings.ConnectionSettings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در بارگذاری تنظیمات: {ex.Message}");
                // در صورت بروز خطا، از تنظیمات پیش‌فرض استفاده می‌کنیم
            }
        }

        /// <summary>
        /// ذخیره تنظیمات
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    ServerUrl = txtServerUrl.Text,
                    ImageQuality = (int)sldQuality.Value,
                    ConnectionSettings = _connectionService.ConnectionInfo.Settings
                };

                string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // اطمینان از وجود پوشه
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath));

                // ذخیره فایل
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ذخیره تنظیمات: {ex.Message}");
            }
        }

        #region رویدادهای سرویس اتصال

        /// <summary>
        /// رویداد تغییر وضعیت اتصال
        /// </summary>
        private void ConnectionService_ConnectionStatusChanged(object sender, bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    // به‌روزرسانی رابط کاربری برای حالت متصل
                    txtConnectionStatus.Text = "متصل";
                    txtConnectionStatus.Foreground = new SolidColorBrush(Colors.Green);

                    txtStatusConnection.Text = "متصل";
                    txtStatusConnection.Foreground = new SolidColorBrush(Colors.Green);

                    // فعال‌سازی دکمه‌ها
                    btnConnectToServer.IsEnabled = false;
                    txtServerUrl.IsEnabled = false;
                    menuConnect.IsEnabled = false;
                    menuDisconnect.IsEnabled = true;
                    menuRequestControl.IsEnabled = true;
                    btnConnectToClient.IsEnabled = true;

                    // ذخیره تنظیمات
                    SaveSettings();

                    UpdateStatus("اتصال به سرور برقرار شد");
                    AddSystemMessage($"اتصال به سرور {_connectionService.ConnectionInfo.ServerUrl} برقرار شد");
                }
                else
                {
                    // به‌روزرسانی رابط کاربری برای حالت قطع
                    txtConnectionStatus.Text = "قطع";
                    txtConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);

                    txtStatusConnection.Text = "قطع";
                    txtStatusConnection.Foreground = new SolidColorBrush(Colors.Red);

                    // فعال‌سازی دکمه‌ها
                    btnConnectToServer.IsEnabled = true;
                    txtServerUrl.IsEnabled = true;
                    menuConnect.IsEnabled = true;
                    menuDisconnect.IsEnabled = false;
                    menuRequestControl.IsEnabled = false;
                    btnConnectToClient.IsEnabled = false;
                    btnDisconnectRemote.IsEnabled = false;
                    btnStartCapture.IsEnabled = false;
                    btnStopCapture.IsEnabled = false;
                    menuStopControl.IsEnabled = false;

                    // بازنشانی وضعیت کنترل
                    txtControlStatus.Text = "غیرفعال";
                    txtControlStatus.Foreground = new SolidColorBrush(Colors.Gray);

                    txtStatusRemoteControl.Text = "غیرفعال";
                    txtStatusRemoteControl.Foreground = new SolidColorBrush(Colors.Gray);

                    // پنهان کردن تصویر صفحه
                    imgRemoteScreen.Source = null;
                    txtNoConnection.Visibility = Visibility.Visible;

                    UpdateStatus("اتصال از سرور قطع شد");
                    AddSystemMessage("اتصال از سرور قطع شد");
                }
            });
        }

        /// <summary>
        /// رویداد دریافت پیام از سرور
        /// </summary>
        private void ConnectionService_MessageReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                AddSystemMessage(message);
            });
        }

        /// <summary>
        /// رویداد دریافت داده صفحه نمایش
        /// </summary>
        private void ConnectionService_ScreenDataReceived(object sender, byte[] screenData)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // تبدیل داده به تصویر و نمایش
                    BitmapImage bitmapImage = _screenCaptureService.ByteArrayToBitmapImage(screenData);

                    if (bitmapImage != null)
                    {
                        imgRemoteScreen.Source = bitmapImage;

                        // نمایش تصویر و مخفی کردن پیام عدم اتصال
                        if (txtNoConnection.Visibility == Visibility.Visible)
                        {
                            txtNoConnection.Visibility = Visibility.Collapsed;
                        }

                        // به‌روزرسانی شمارنده فریم
                        _frameCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"خطا در پردازش داده تصویر: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// رویداد خطا در سرویس اتصال
        /// </summary>
        private void ConnectionService_ErrorOccurred(object sender, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"خطا: {errorMessage}");
                AddSystemMessage($"خطا: {errorMessage}");
            });
        }

        /// <summary>
        /// رویداد دریافت درخواست کنترل از راه دور
        /// </summary>
        private void ConnectionService_RemoteControlRequestReceived(object sender, RemoteControlRequestEventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                // در صورت تنظیم پذیرش خودکار، بدون نمایش پنجره پذیرش انجام می‌شود
                if (_connectionService.ConnectionInfo.Settings.AutoAcceptConnections)
                {
                    _connectionService.AcceptRemoteControlRequestAsync(args.ConnectionId).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                OnError($"خطا در پذیرش درخواست: {task.Exception.InnerException?.Message}");
                            });
                        }
                    });

                    AddSystemMessage($"درخواست کنترل از راه دور از طرف {args.SupportUserName} به صورت خودکار پذیرفته شد");
                    UpdateStatus("درخواست کنترل از راه دور پذیرفته شد");
                    return;
                }

                // نمایش پنجره درخواست
                _remoteControlRequestWindow = new RemoteControlRequestWindow(args.SupportUserName, args.ConnectionId);
                _remoteControlRequestWindow.Owner = this;

                // ثبت رویدادهای پاسخ
                _remoteControlRequestWindow.RequestAccepted += (s, connectionId) =>
                {
                    _connectionService.AcceptRemoteControlRequestAsync(connectionId).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                OnError($"خطا در پذیرش درخواست: {task.Exception.InnerException?.Message}");
                            });
                        }
                    });
                };

                _remoteControlRequestWindow.RequestRejected += (s, connectionId) =>
                {
                    _connectionService.RejectRemoteControlRequestAsync(connectionId).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                OnError($"خطا در رد درخواست: {task.Exception.InnerException?.Message}");
                            });
                        }
                    });
                };

                // نمایش پنجره
                _remoteControlRequestWindow.Show();

                // پخش صدای اعلان
                if (_connectionService.ConnectionInfo.Settings.PlaySoundOnConnection)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                }

                // به‌روزرسانی پیام‌ها
                AddSystemMessage($"درخواست کنترل از راه دور از طرف {args.SupportUserName} دریافت شد");
                UpdateStatus("درخواست کنترل از راه دور دریافت شد");
            });
        }

        /// <summary>
        /// رویداد تغییر وضعیت کنترل از راه دور
        /// </summary>
        private void ConnectionService_RemoteControlStatusChanged(object sender, RemoteControlStatusEventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if (args.IsControlling)
                {
                    // در حال کنترل کامپیوتر دیگر
                    txtControlStatus.Text = $"در حال کنترل {args.RemoteClientName ?? args.RemoteClientId}";
                    txtControlStatus.Foreground = new SolidColorBrush(Colors.Green);

                    txtStatusRemoteControl.Text = "کنترل کننده";
                    txtStatusRemoteControl.Foreground = new SolidColorBrush(Colors.Green);

                    // فعال‌سازی دکمه‌ها
                    btnDisconnectRemote.IsEnabled = true;
                    menuStopControl.IsEnabled = true;

                    // فعال‌سازی سرویس ورودی
                    _inputService.EnableInput(_connectionService);

                    UpdateStatus("کنترل از راه دور فعال شد");
                    AddSystemMessage($"کنترل از راه دور برای {args.RemoteClientName ?? args.RemoteClientId} فعال شد");

                    // تغییر نوع اتصال
                    _connectionService.ConnectionInfo.ConnectionType = ConnectionType.Controller;
                }
                else if (args.IsBeingControlled)
                {
                    // در حال کنترل شدن
                    txtControlStatus.Text = "در حال کنترل شدن";
                    txtControlStatus.Foreground = new SolidColorBrush(Colors.Orange);

                    txtStatusRemoteControl.Text = "تحت کنترل";
                    txtStatusRemoteControl.Foreground = new SolidColorBrush(Colors.Orange);

                    // فعال‌سازی دکمه‌های ارسال تصویر
                    btnStartCapture.IsEnabled = true;
                    btnStopCapture.IsEnabled = false;

                    // فعال‌سازی امکان توقف اتصال
                    btnDisconnectRemote.IsEnabled = true;
                    menuStopControl.IsEnabled = true;

                    UpdateStatus("کامپیوتر شما در حال کنترل از راه دور است");
                    AddSystemMessage("کامپیوتر شما در حال کنترل از راه دور است");

                    // تغییر نوع اتصال
                    _connectionService.ConnectionInfo.ConnectionType = ConnectionType.Controlled;

                    // شروع خودکار ارسال تصویر
                    BtnStartCapture_Click(this, null);

                    // پخش صدای اعلان
                    if (_connectionService.ConnectionInfo.Settings.PlaySoundOnConnection)
                    {
                        System.Media.SystemSounds.Beep.Play();
                    }
                }
                else
                {
                    // حالت عادی - بدون کنترل
                    txtControlStatus.Text = "غیرفعال";
                    txtControlStatus.Foreground = new SolidColorBrush(Colors.Gray);

                    txtStatusRemoteControl.Text = "غیرفعال";
                    txtStatusRemoteControl.Foreground = new SolidColorBrush(Colors.Gray);

                    // غیرفعال‌سازی دکمه‌ها
                    btnDisconnectRemote.IsEnabled = false;
                    btnStartCapture.IsEnabled = false;
                    btnStopCapture.IsEnabled = false;
                    menuStopControl.IsEnabled = false;

                    // غیرفعال‌سازی سرویس ورودی
                    _inputService.DisableInput();

                    // توقف ارسال تصویر
                    if (_screenCaptureService.IsCapturing)
                    {
                        _screenCaptureService.StopCapture();
                    }

                    // پاکسازی تصویر
                    imgRemoteScreen.Source = null;
                    txtNoConnection.Visibility = Visibility.Visible;

                    UpdateStatus("کنترل از راه دور غیرفعال شد");
                    AddSystemMessage("کنترل از راه دور غیرفعال شد");

                    // تغییر نوع اتصال
                    _connectionService.ConnectionInfo.ConnectionType = ConnectionType.Unknown;
                }
            });
        }

        #endregion

        #region رویدادهای سرویس ضبط صفحه

        /// <summary>
        /// رویداد ضبط تصویر صفحه
        /// </summary>
        private void ScreenCaptureService_ScreenCaptured(object sender, byte[] screenData)
        {
            // به‌روزرسانی شمارنده فریم‌ها
            _frameCount++;
        }

        /// <summary>
        /// رویداد خطا در ضبط تصویر صفحه
        /// </summary>
        private void ScreenCaptureService_CaptureError(object sender, Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                OnError($"خطا در ضبط تصویر صفحه: {ex.Message}");

                // غیرفعال‌سازی ضبط در صورت بروز خطا
                btnStartCapture.IsEnabled = true;
                btnStopCapture.IsEnabled = false;
            });
        }

        #endregion

        #region رویدادهای سرویس ورودی

        /// <summary>
        /// رویداد پردازش ورودی
        /// </summary>
        private void InputService_InputProcessed(object sender, string message)
        {
            // در حالت دیباگ می‌توان پیام‌ها را نمایش داد
            Debug.WriteLine($"ورودی: {message}");
        }

        #endregion

        #region رویدادهای صفحه

        /// <summary>
        /// رویداد فشردن کلید در پنجره اصلی
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // ارسال کلیدهای فشرده شده به کلاینت راه دور
            if (_connectionService.IsControlling && _inputService != null)
            {
                // فقط اگر فوکوس روی تصویر یا خود پنجره باشد
                if (e.Source == imgRemoteScreen || e.Source == this)
                {
                    _inputService.HandleKeyDown(e.Key).ConfigureAwait(false);
                    e.Handled = true;
                }
            }

            // کلیدهای میانبر برنامه
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D:
                        // قطع اتصال از راه دور (Ctrl+D)
                        if (btnDisconnectRemote.IsEnabled)
                        {
                            BtnDisconnectRemote_Click(sender, e);
                            e.Handled = true;
                        }
                        break;

                    case Key.C:
                        // اتصال به سرور (Ctrl+C)
                        if (btnConnectToServer.IsEnabled)
                        {
                            BtnConnectToServer_Click(sender, e);
                            e.Handled = true;
                        }
                        break;

                    case Key.R:
                        // درخواست کنترل از راه دور (Ctrl+R)
                        if (menuRequestControl.IsEnabled)
                        {
                            MenuRequestControl_Click(sender, e);
                            e.Handled = true;
                        }
                        break;
                }
            }
        }

        // در MainWindow.xaml.cs
        // در MainWindow.xaml.cs
        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            // نمایش پنجره تنظیمات
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;

            // ارسال تنظیمات به عنوان ویژگی‌های موقت
            settingsWindow.Tag = new Tuple<ConnectionInfo, ScreenCaptureService>(_connectionService.ConnectionInfo, _screenCaptureService);

            if (settingsWindow.ShowDialog() == true)
            {
                // ذخیره تنظیمات
                SaveSettings();
            }
        }

        // در کلاس SettingsWindow - متد Window_Loaded
        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // دریافت اطلاعات از Tag
        //    if (this.Tag is Tuple<ConnectionInfo, ScreenCaptureService> settings)
        //    {
        //        var connectionInfo = settings.Item1;
        //        var screenCaptureService = settings.Item2;

        //        // پر کردن کنترل‌ها
        //        // ...
        //    }
        //}

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            // خروج از برنامه
            Close();
        }

        private void MenuConnect_Click(object sender, RoutedEventArgs e)
        {
            // اتصال به سرور
            BtnConnectToServer_Click(sender, e);
        }

        private void MenuDisconnect_Click(object sender, RoutedEventArgs e)
        {
            // قطع اتصال از سرور
            DisconnectFromServer();
        }

        private void MenuRequestControl_Click(object sender, RoutedEventArgs e)
        {
            // نمایش پنجره اتصال به کامپیوتر دیگر
            ConnectToRemoteWindow connectWindow = new ConnectToRemoteWindow();
            connectWindow.Owner = this;

            if (connectWindow.ShowDialog() == true)
            {
                // درخواست کنترل با اطلاعات دریافتی
                RequestRemoteControl(connectWindow.RemoteClientId, connectWindow.AccessKey);

                // ذخیره اطلاعات در صورت درخواست کاربر
                if (connectWindow.SaveCredentials)
                {
                    SaveRemoteCredentials(connectWindow.RemoteClientId, connectWindow.AccessKey);
                }
            }
        }

        private void MenuStopControl_Click(object sender, RoutedEventArgs e)
        {
            // توقف کنترل از راه دور
            StopRemoteControl();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            // نمایش پنجره درباره برنامه
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void BtnCopyId_Click(object sender, RoutedEventArgs e)
        {
            // کپی شناسه در کلیپ بورد
            Clipboard.SetText(txtMyId.Text);
            UpdateStatus("شناسه در کلیپ بورد کپی شد");

            // نمایش انیمیشن تأیید
            ShowCopyAnimation(btnCopyId);
        }

        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            // نمایش/عدم نمایش رمز عبور
            if (txtMyPassword.Visibility == Visibility.Visible)
            {
                // نمایش رمز در تکست باکس عادی
                TextBox textBox = new TextBox();
                textBox.Text = txtMyPassword.Password;
                Grid.SetRow(textBox, Grid.GetRow(txtMyPassword));
                Grid.SetColumn(textBox, Grid.GetColumn(txtMyPassword));
                textBox.Margin = txtMyPassword.Margin;
                textBox.Name = "txtPasswordVisible";

                // جایگزینی کنترل
                Grid parent = (Grid)txtMyPassword.Parent;
                int index = parent.Children.IndexOf(txtMyPassword);
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, textBox);

                // تغییر متن دکمه
                btnShowPassword.Content = "پنهان کردن";
            }
            else
            {
                // بازگرداندن به حالت پسورد باکس
                TextBox textBox = FindVisualChild<TextBox>(this, "txtPasswordVisible");
                if (textBox != null)
                {
                    // ایجاد پسورد باکس
                    PasswordBox passwordBox = new PasswordBox();
                    passwordBox.Password = textBox.Text;
                    passwordBox.IsEnabled = false;
                    passwordBox.Margin = textBox.Margin;
                    passwordBox.Name = "txtMyPassword";

                    // جایگزینی کنترل
                    Grid parent = (Grid)textBox.Parent;
                    int index = parent.Children.IndexOf(textBox);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, passwordBox);

                    // تغییر متن دکمه
                    btnShowPassword.Content = "نمایش";

                    // ذخیره مرجع جدید
                    txtMyPassword = passwordBox;
                }
            }
        }

        private async void BtnConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // اتصال به سرور
                string serverUrl = txtServerUrl.Text.Trim();

                if (string.IsNullOrEmpty(serverUrl))
                {
                    MessageBox.Show("لطفاً آدرس سرور را وارد کنید", "خطای اتصال", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // اتصال به سرور
                await ConnectToServer(serverUrl);
            }
            catch (Exception ex)
            {
                OnError($"خطا در اتصال به سرور: {ex.Message}");
                btnConnectToServer.IsEnabled = true;
            }
        }

        private async void BtnConnectToClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // اتصال به کلاینت دیگر
                string remoteId = txtRemoteId.Text.Trim();
                string remotePassword = txtRemotePassword.Password;

                if (string.IsNullOrEmpty(remoteId) || string.IsNullOrEmpty(remotePassword))
                {
                    MessageBox.Show("لطفاً شناسه و رمز ورود کاربر مقصد را وارد کنید", "خطای اتصال", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // درخواست کنترل
                await RequestRemoteControl(remoteId, remotePassword);
            }
            catch (Exception ex)
            {
                OnError($"خطا در ارسال درخواست: {ex.Message}");
            }
        }

        private async void BtnDisconnectRemote_Click(object sender, RoutedEventArgs e)
        {
            // قطع کنترل از راه دور
            await StopRemoteControl();
        }

        private void BtnStartCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // شروع ارسال تصویر صفحه نمایش
                if (!_screenCaptureService.IsCapturing)
                {
                    _screenCaptureService.StartCapture(_connectionService);
                    btnStartCapture.IsEnabled = false;
                    btnStopCapture.IsEnabled = true;
                    AddSystemMessage("ارسال تصویر صفحه نمایش آغاز شد");
                }
            }
            catch (Exception ex)
            {
                OnError($"خطا در شروع ارسال تصویر: {ex.Message}");
            }
        }

        private void BtnStopCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // توقف ارسال تصویر صفحه نمایش
                if (_screenCaptureService.IsCapturing)
                {
                    _screenCaptureService.StopCapture();
                    btnStartCapture.IsEnabled = true;
                    btnStopCapture.IsEnabled = false;
                    AddSystemMessage("ارسال تصویر صفحه نمایش متوقف شد");
                }
            }
            catch (Exception ex)
            {
                OnError($"خطا در توقف ارسال تصویر: {ex.Message}");
            }
        }

        private void SldQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtQualityValue != null)
            {
                int quality = (int)sldQuality.Value;
                txtQualityValue.Text = $"{quality}%";

                // اعمال تغییر کیفیت بر روی سرویس ضبط صفحه
                _screenCaptureService.Quality = quality;

                // ذخیره تنظیمات
                SaveSettings();
            }
        }

        private void ImgRemoteScreen_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // ارسال رویداد حرکت ماوس به سرویس ورودی
            if (_connectionService.IsControlling)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseMove(position);
            }
        }

        private void ImgRemoteScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // ارسال رویداد کلیک ماوس به سرویس ورودی
            if (_connectionService.IsControlling)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseDown(position, e.ChangedButton);

                // دریافت فوکوس برای دریافت کلیدهای ورودی
                imgRemoteScreen.Focus();
            }
        }

        private void ImgRemoteScreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // ارسال رویداد رها کردن ماوس به سرویس ورودی
            if (_connectionService.IsControlling)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseUp(position, e.ChangedButton);
            }
        }

        private void ImgRemoteScreen_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // ارسال رویداد چرخش چرخ ماوس به سرویس ورودی
            if (_connectionService.IsControlling)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseWheel(position, e.Delta);
            }
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            // به‌روزرسانی ادواری وضعیت سیستم
            if (_connectionService.ConnectionInfo.IsConnected)
            {
                string statusText = $"متصل به {_connectionService.ConnectionInfo.ServerUrl}";

                if (_connectionService.IsControlling)
                {
                    statusText += $" | در حال کنترل {_connectionService.ConnectionInfo.RemoteClientId}";
                }
                else if (_connectionService.IsBeingControlled)
                {
                    statusText += " | در حال کنترل شدن";
                }

                UpdateStatus(statusText);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // پرسش از کاربر در صورت فعال بودن کنترل از راه دور
                if (_connectionService.IsControlling || _connectionService.IsBeingControlled)
                {
                    var result = MessageBox.Show(
                        "کنترل از راه دور فعال است. آیا مطمئن هستید که می‌خواهید برنامه را ببندید؟",
                        "بستن برنامه",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                // ذخیره تنظیمات
                SaveSettings();

                // پاک‌سازی منابع هنگام بستن برنامه
                _fpsTimer.Stop();
                _statusTimer.Stop();

                // توقف ارسال تصویر در صورت فعال بودن
                if (_screenCaptureService.IsCapturing)
                {
                    _screenCaptureService.StopCapture();
                }

                // قطع اتصال از سرور
                if (_connectionService.ConnectionInfo.IsConnected)
                {
                    try
                    {
                        _connectionService.DisconnectAsync().Wait();
                    }
                    catch
                    {
                        // نادیده گرفتن خطاها هنگام بستن برنامه
                    }
                }

                // آزادسازی منابع
                _connectionService.Dispose();
                _screenCaptureService.Dispose();
                _inputService.Dispose();
            }
            catch
            {
                // نادیده گرفتن خطاها هنگام بستن برنامه
            }
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            // به‌روزرسانی شمارنده FPS
            var elapsed = DateTime.Now - _lastFpsUpdate;
            if (elapsed.TotalSeconds >= 1)
            {
                txtFps.Text = _frameCount.ToString();
                _frameCount = 0;
                _lastFpsUpdate = DateTime.Now;
            }
        }

        #endregion

        #region متدهای کمکی

        /// <summary>
        /// به‌روزرسانی وضعیت در نوار وضعیت
        /// </summary>
        private void UpdateStatus(string message)
        {
            txtStatusMessage.Text = message;
        }

        /// <summary>
        /// افزودن پیام به لیست پیام‌های سیستم
        /// </summary>
        private void AddSystemMessage(string message)
        {
            ListBoxItem item = new ListBoxItem();
            item.Content = $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}";

            // افزودن به لیست و اسکرول به آخرین آیتم
            lstSystemMessages.Items.Add(item);
            lstSystemMessages.ScrollIntoView(item);

            // محدود کردن تعداد پیام‌ها به 100 مورد
            while (lstSystemMessages.Items.Count > 100)
            {
                lstSystemMessages.Items.RemoveAt(0);
            }
        }

        /// <summary>
        /// نمایش پیام خطا
        /// </summary>
        private void OnError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus($"خطا: {errorMessage}");
            AddSystemMessage($"خطا: {errorMessage}");
        }

        /// <summary>
        /// اتصال به سرور
        /// </summary>
        private async Task ConnectToServer(string serverUrl)
        {
            try
            {
                // غیرفعال‌سازی دکمه و به‌روزرسانی وضعیت
                btnConnectToServer.IsEnabled = false;
                UpdateStatus("در حال اتصال به سرور...");

                // اتصال به سرور
                await _connectionService.InitializeConnectionAsync(serverUrl);

                // ذخیره آدرس سرور در تنظیمات
                _connectionService.ConnectionInfo.ServerUrl = serverUrl;
                SaveSettings();
            }
            catch (Exception ex)
            {
                OnError($"خطا در اتصال به سرور: {ex.Message}");
                btnConnectToServer.IsEnabled = true;
                throw;
            }
        }

        /// <summary>
        /// قطع اتصال از سرور
        /// </summary>
        private async void DisconnectFromServer()
        {
            try
            {
                // به‌روزرسانی وضعیت
                UpdateStatus("در حال قطع اتصال از سرور...");

                // غیرفعال‌سازی دکمه‌ها
                menuDisconnect.IsEnabled = false;
                btnConnectToServer.IsEnabled = false;

                // قطع کنترل از راه دور در صورت فعال بودن
                if (_connectionService.IsControlling || _connectionService.IsBeingControlled)
                {
                    await _connectionService.StopRemoteControlAsync();
                }

                // قطع اتصال از سرور
                await _connectionService.DisconnectAsync();

                // به‌روزرسانی دکمه‌ها
                btnConnectToServer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                OnError($"خطا در قطع اتصال: {ex.Message}");
                btnConnectToServer.IsEnabled = true;
            }
        }

        /// <summary>
        /// درخواست کنترل از راه دور
        /// </summary>
        private async Task RequestRemoteControl(string clientId, string accessKey)
        {
            try
            {
                // بررسی وضعیت اتصال
                if (!_connectionService.ConnectionInfo.IsConnected)
                {
                    OnError("ابتدا به سرور متصل شوید");
                    return;
                }

                // به‌روزرسانی وضعیت
                UpdateStatus($"در حال ارسال درخواست کنترل از راه دور به {clientId}...");

                // ارسال درخواست
                await _connectionService.RequestRemoteControlAsync(clientId, accessKey);

                // پاکسازی فیلدها
                txtRemotePassword.Password = "";
            }
            catch (Exception ex)
            {
                OnError($"خطا در ارسال درخواست کنترل از راه دور: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// توقف کنترل از راه دور
        /// </summary>
        private async Task StopRemoteControl()
        {
            try
            {
                // به‌روزرسانی وضعیت
                UpdateStatus("در حال قطع کنترل از راه دور...");

                // غیرفعال‌سازی دکمه‌ها
                btnDisconnectRemote.IsEnabled = false;
                menuStopControl.IsEnabled = false;

                // قطع کنترل از راه دور
                await _connectionService.StopRemoteControlAsync();
            }
            catch (Exception ex)
            {
                OnError($"خطا در قطع کنترل از راه دور: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ذخیره اطلاعات اتصال به کامپیوتر راه دور
        /// </summary>
        private void SaveRemoteCredentials(string clientId, string accessKey)
        {
            try
            {
                var savedConnections = new List<SavedConnection>();
                string savedConnectionsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RemoteDesktopClient",
                    "saved_connections.json");

                // بارگذاری اتصالات ذخیره شده قبلی
                if (File.Exists(savedConnectionsPath))
                {
                    string json = File.ReadAllText(savedConnectionsPath);
                    savedConnections = System.Text.Json.JsonSerializer.Deserialize<List<SavedConnection>>(json) ?? new List<SavedConnection>();
                }

                // بررسی وجود اتصال قبلی با همین شناسه
                var existingConnection = savedConnections.FirstOrDefault(c => c.ClientId == clientId);
                if (existingConnection != null)
                {
                    // به‌روزرسانی اتصال موجود
                    existingConnection.AccessKey = accessKey;
                    existingConnection.LastConnected = DateTime.Now;
                }
                else
                {
                    // افزودن اتصال جدید
                    savedConnections.Add(new SavedConnection
                    {
                        ClientId = clientId,
                        AccessKey = accessKey,
                        Label = $"Client {clientId}",
                        LastConnected = DateTime.Now
                    });
                }

                // ذخیره اتصالات
                string updatedJson = System.Text.Json.JsonSerializer.Serialize(savedConnections, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // اطمینان از وجود پوشه
                Directory.CreateDirectory(Path.GetDirectoryName(savedConnectionsPath));

                // ذخیره فایل
                File.WriteAllText(savedConnectionsPath, updatedJson);

                AddSystemMessage($"اطلاعات اتصال به کلاینت {clientId} ذخیره شد");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ذخیره اطلاعات اتصال: {ex.Message}");
            }
        }

        /// <summary>
        /// نمایش انیمیشن تأیید پس از کپی
        /// </summary>
        private void ShowCopyAnimation(Button button)
        {
            var originalContent = button.Content;
            button.Content = "✓";

            // ایجاد تایمر برای بازگرداندن محتوای اصلی
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (sender, args) =>
            {
                button.Content = originalContent;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// یافتن اولین فرزند ویژوال از نوع T با نام مشخص
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // تعداد فرزندان ویژوال
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            // بررسی همه فرزندان
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                // اگر فرزند از نوع مورد نظر باشد و نام مطابقت داشته باشد
                if (child is T typedChild &&
                    child is FrameworkElement element &&
                    element.Name == childName)
                {
                    return typedChild;
                }

                // جستجوی بازگشتی
                T childOfChild = FindVisualChild<T>(child, childName);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// تنظیمات برنامه
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// آدرس سرور
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// کیفیت تصویر
        /// </summary>
        public int ImageQuality { get; set; }

        /// <summary>
        /// تنظیمات اتصال
        /// </summary>
        public ConnectionSettings ConnectionSettings { get; set; }
    }

    /// <summary>
    /// اطلاعات اتصال ذخیره شده
    /// </summary>
    public class SavedConnection
    {
        /// <summary>
        /// شناسه کلاینت
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// کلید دسترسی
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// برچسب (نام مستعار)
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// زمان آخرین اتصال
        /// </summary>
        public DateTime LastConnected { get; set; }
    }
}