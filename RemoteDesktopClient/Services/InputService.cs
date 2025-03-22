using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using RemoteDesktopClient.Services;

namespace RemoteDesktopClient.Services
{
    /// <summary>
    /// سرویس مدیریت ورودی‌های کاربر (ماوس و کیبورد) در کنترل از راه دور
    /// </summary>
    public class InputService : IDisposable
    {
        private ConnectionService _connectionService;
        private bool _isInputEnabled;

        // مدیریت وضعیت کلیدهای کنترلی
        private bool _isCtrlPressed;
        private bool _isAltPressed;
        private bool _isShiftPressed;

        // رویدادها
        public event EventHandler<string> InputProcessed;

        /// <summary>
        /// سازنده کلاس
        /// </summary>
        public InputService()
        {
            _isInputEnabled = false;
            _isCtrlPressed = false;
            _isAltPressed = false;
            _isShiftPressed = false;
        }

        /// <summary>
        /// فعال‌سازی دریافت و ارسال ورودی
        /// </summary>
        public void EnableInput(ConnectionService connectionService)
        {
            _connectionService = connectionService;
            _isInputEnabled = true;

            // ثبت رویدادهای دریافت ورودی از راه دور
            RegisterRemoteEvents();

            LogMessage("پردازش ورودی فعال شد");
        }

        /// <summary>
        /// غیرفعال‌سازی دریافت و ارسال ورودی
        /// </summary>
        public void DisableInput()
        {
            _isInputEnabled = false;

            // لغو ثبت رویدادها
            UnregisterRemoteEvents();

            // بازنشانی وضعیت کلیدهای کنترلی
            _isCtrlPressed = false;
            _isAltPressed = false;
            _isShiftPressed = false;

            LogMessage("پردازش ورودی غیرفعال شد");
        }

        /// <summary>
        /// ثبت رویدادهای دریافت ورودی از راه دور
        /// </summary>
        private void RegisterRemoteEvents()
        {
            if (_connectionService != null)
            {
                _connectionService.MouseEventReceived += ConnectionService_MouseEventReceived;
                _connectionService.KeyboardEventReceived += ConnectionService_KeyboardEventReceived;
            }
        }

        /// <summary>
        /// لغو ثبت رویدادهای دریافت ورودی
        /// </summary>
        private void UnregisterRemoteEvents()
        {
            if (_connectionService != null)
            {
                _connectionService.MouseEventReceived -= ConnectionService_MouseEventReceived;
                _connectionService.KeyboardEventReceived -= ConnectionService_KeyboardEventReceived;
            }
        }

        /// <summary>
        /// رویداد دریافت رویداد ماوس از راه دور
        /// </summary>
        private void ConnectionService_MouseEventReceived(object sender, RemoteMouseEventArgs e)
        {
            if (!_isInputEnabled)
                return;

            // اجرای رویداد ماوس دریافتی در سیستم
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // ایجاد مختصات نسبی برای سیستم
                    Point screenPoint = new Point(e.X, e.Y);

                    // شبیه‌سازی رویداد ماوس بر اساس نوع آن
                    switch (e.EventType)
                    {
                        case "move":
                            NativeInputSimulator.SetCursorPosition((int)screenPoint.X, (int)screenPoint.Y);
                            break;

                        case "down":
                            NativeInputSimulator.SetCursorPosition((int)screenPoint.X, (int)screenPoint.Y);
                            NativeInputSimulator.MouseDown(e.Button);
                            break;

                        case "up":
                            NativeInputSimulator.SetCursorPosition((int)screenPoint.X, (int)screenPoint.Y);
                            NativeInputSimulator.MouseUp(e.Button);
                            break;

                        case "wheel":
                            NativeInputSimulator.SetCursorPosition((int)screenPoint.X, (int)screenPoint.Y);
                            NativeInputSimulator.MouseWheel(e.Button); // از مقدار Button برای جهت و مقدار چرخش استفاده می‌کنیم
                            break;
                    }

                    LogMessage($"اجرای رویداد ماوس: {e.EventType} در موقعیت {e.X},{e.Y}");
                }
                catch (Exception ex)
                {
                    LogMessage($"خطا در اجرای رویداد ماوس: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// رویداد دریافت رویداد کیبورد از راه دور
        /// </summary>
        private void ConnectionService_KeyboardEventReceived(object sender, KeyboardEventArgs e)
        {
            if (!_isInputEnabled)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // تبدیل کد کلید به کد ویندوز
                    int virtualKeyCode = ConvertKeyCodeToVirtualKey(e.KeyCode);

                    // شبیه‌سازی رویداد کیبورد
                    if (e.IsKeyDown)
                    {
                        NativeInputSimulator.KeyDown(virtualKeyCode);

                        // ثبت وضعیت کلیدهای کنترلی
                        UpdateModifierKeyState(virtualKeyCode, true);
                    }
                    else
                    {
                        NativeInputSimulator.KeyUp(virtualKeyCode);

                        // به‌روزرسانی وضعیت کلیدهای کنترلی
                        UpdateModifierKeyState(virtualKeyCode, false);
                    }

                    LogMessage($"اجرای رویداد کیبورد: کلید {e.KeyCode} {(e.IsKeyDown ? "فشرده" : "رها")} شد");
                }
                catch (Exception ex)
                {
                    LogMessage($"خطا در اجرای رویداد کیبورد: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// به‌روزرسانی وضعیت کلیدهای کنترلی (Ctrl, Alt, Shift)
        /// </summary>
        private void UpdateModifierKeyState(int virtualKeyCode, bool isKeyDown)
        {
            switch (virtualKeyCode)
            {
                case 0x10: // VK_SHIFT
                    _isShiftPressed = isKeyDown;
                    break;
                case 0x11: // VK_CONTROL
                    _isCtrlPressed = isKeyDown;
                    break;
                case 0x12: // VK_ALT
                    _isAltPressed = isKeyDown;
                    break;
            }
        }

        /// <summary>
        /// پردازش رویداد حرکت ماوس
        /// </summary>
        public void HandleMouseMove(Point position)
        {
            if (!_isInputEnabled || _connectionService == null)
                return;

            try
            {
                _connectionService.SendMouseEventAsync(
                    (int)position.X,
                    (int)position.Y,
                    "move"
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد حرکت ماوس: {ex.Message}");
            }
        }

        /// <summary>
        /// پردازش رویداد کلیک ماوس
        /// </summary>
        public void HandleMouseDown(Point position, MouseButton button)
        {
            if (!_isInputEnabled || _connectionService == null)
                return;

            try
            {
                _connectionService.SendMouseEventAsync(
                    (int)position.X,
                    (int)position.Y,
                    "down",
                    ConvertMouseButtonToValue(button)
                ).ConfigureAwait(false);

                LogMessage($"ارسال رویداد کلیک ماوس: دکمه {button} در موقعیت {position.X},{position.Y}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد کلیک ماوس: {ex.Message}");
            }
        }

        /// <summary>
        /// پردازش رویداد رها کردن دکمه ماوس
        /// </summary>
        public void HandleMouseUp(Point position, MouseButton button)
        {
            if (!_isInputEnabled || _connectionService == null)
                return;

            try
            {
                _connectionService.SendMouseEventAsync(
                    (int)position.X,
                    (int)position.Y,
                    "up",
                    ConvertMouseButtonToValue(button)
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد رها کردن ماوس: {ex.Message}");
            }
        }

        /// <summary>
        /// پردازش رویداد چرخش چرخ ماوس
        /// </summary>
        public void HandleMouseWheel(Point position, int delta)
        {
            if (!_isInputEnabled || _connectionService == null)
                return;

            try
            {
                _connectionService.SendMouseEventAsync(
                    (int)position.X,
                    (int)position.Y,
                    "wheel",
                    delta
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد چرخش ماوس: {ex.Message}");
            }
        }

        /// <summary>
        /// پردازش رویداد فشردن کلید
        /// </summary>
        public async Task HandleKeyDown(Key key)
        {
            if (!_isInputEnabled || _connectionService == null)
                return;

            try
            {
                await _connectionService.SendKeyboardEventAsync(
                    ConvertKeyToString(key),
                    true
                );

                LogMessage($"ارسال رویداد فشردن کلید: {key}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد فشردن کلید: {ex.Message}");
            }
        }

        /// <summary>
        /// پردازش رویداد رها کردن کلید
        /// </summary>
        public async Task HandleKeyUp(Key key)
        {
            if (!_isInputEnabled || _connectionService == null)
                return;

            try
            {
                await _connectionService.SendKeyboardEventAsync(
                    ConvertKeyToString(key),
                    false
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطا در ارسال رویداد رها کردن کلید: {ex.Message}");
            }
        }

        /// <summary>
        /// تبدیل دکمه ماوس به مقدار عددی
        /// </summary>
        private int ConvertMouseButtonToValue(MouseButton button)
        {

            switch (button)
            {
                case MouseButton.Left:
                    return 0;
                case MouseButton.Right:
                    return 2;
                case MouseButton.Middle:
                    return 1;
                case MouseButton.XButton1:
                    return 3;
                case MouseButton.XButton2:
                    return 4;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// تبدیل کلید به رشته قابل استفاده در جاوااسکریپت
        /// </summary>
        private string ConvertKeyToString(Key key)
        {
            switch (key)
            {
                // کلیدهای کنترلی
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    return "Control";
                case Key.LeftShift:
                case Key.RightShift:
                    return "Shift";
                case Key.LeftAlt:
                case Key.RightAlt:
                    return "Alt";
                case Key.LWin:
                case Key.RWin:
                    return "Meta";

                // کلیدهای ویژه
                case Key.Back:
                    return "Backspace";
                case Key.Tab:
                    return "Tab";
                case Key.Enter:
                    return "Enter";
                case Key.Escape:
                    return "Escape";
                case Key.Space:
                    return " ";
                case Key.PageUp:
                    return "PageUp";
                case Key.PageDown:
                    return "PageDown";
                case Key.End:
                    return "End";
                case Key.Home:
                    return "Home";
                case Key.Left:
                    return "ArrowLeft";
                case Key.Up:
                    return "ArrowUp";
                case Key.Right:
                    return "ArrowRight";
                case Key.Down:
                    return "ArrowDown";
                case Key.Insert:
                    return "Insert";
                case Key.Delete:
                    return "Delete";

                // سایر کلیدها
                default:
                    // برای کلیدهای حروف و اعداد، از نام کلید استفاده می‌کنیم
                    return key.ToString();
            }
        }

        /// <summary>
        /// تبدیل کد کلید به کد مجازی ویندوز
        /// </summary>
        private int ConvertKeyCodeToVirtualKey(string keyCode)
        {
            switch (keyCode)
            {
                // کلیدهای کنترلی
                case "Control":
                    return 0x11; // VK_CONTROL
                case "Shift":
                    return 0x10; // VK_SHIFT
                case "Alt":
                    return 0x12; // VK_ALT
                case "Meta":
                    return 0x5B; // VK_LWIN

                // کلیدهای ویژه
                case "Backspace":
                    return 0x08; // VK_BACK
                case "Tab":
                    return 0x09; // VK_TAB
                case "Enter":
                    return 0x0D; // VK_RETURN
                case "Escape":
                    return 0x1B; // VK_ESCAPE
                case " ":
                    return 0x20; // VK_SPACE
                case "PageUp":
                    return 0x21; // VK_PRIOR
                case "PageDown":
                    return 0x22; // VK_NEXT
                case "End":
                    return 0x23; // VK_END
                case "Home":
                    return 0x24; // VK_HOME
                case "ArrowLeft":
                    return 0x25; // VK_LEFT
                case "ArrowUp":
                    return 0x26; // VK_UP
                case "ArrowRight":
                    return 0x27; // VK_RIGHT
                case "ArrowDown":
                    return 0x28; // VK_DOWN
                case "Insert":
                    return 0x2D; // VK_INSERT
                case "Delete":
                    return 0x2E; // VK_DELETE

                // اعداد
                case "0":
                    return 0x30;
                case "1":
                    return 0x31;
                case "2":
                    return 0x32;
                case "3":
                    return 0x33;
                case "4":
                    return 0x34;
                case "5":
                    return 0x35;
                case "6":
                    return 0x36;
                case "7":
                    return 0x37;
                case "8":
                    return 0x38;
                case "9":
                    return 0x39;

                // حروف
                case "A":
                    return 0x41;
                case "B":
                    return 0x42;
                case "C":
                    return 0x43;
                case "D":
                    return 0x44;
                case "E":
                    return 0x45;
                case "F":
                    return 0x46;
                case "G":
                    return 0x47;
                case "H":
                    return 0x48;
                case "I":
                    return 0x49;
                case "J":
                    return 0x4A;
                case "K":
                    return 0x4B;
                case "L":
                    return 0x4C;
                case "M":
                    return 0x4D;
                case "N":
                    return 0x4E;
                case "O":
                    return 0x4F;
                case "P":
                    return 0x50;
                case "Q":
                    return 0x51;
                case "R":
                    return 0x52;
                case "S":
                    return 0x53;
                case "T":
                    return 0x54;
                case "U":
                    return 0x55;
                case "V":
                    return 0x56;
                case "W":
                    return 0x57;
                case "X":
                    return 0x58;
                case "Y":
                    return 0x59;
                case "Z":
                    return 0x5A;

                // کلیدهای تابعی
                case "F1":
                    return 0x70;
                case "F2":
                    return 0x71;
                case "F3":
                    return 0x72;
                case "F4":
                    return 0x73;
                case "F5":
                    return 0x74;
                case "F6":
                    return 0x75;
                case "F7":
                    return 0x76;
                case "F8":
                    return 0x77;
                case "F9":
                    return 0x78;
                case "F10":
                    return 0x79;
                case "F11":
                    return 0x7A;
                case "F12":
                    return 0x7B;

                // سایر کلیدها
                default:
                    // تلاش برای تبدیل به حرف (فقط برای کلیدهای یک حرفی)
                    if (keyCode.Length == 1 && char.IsLetter(keyCode[0]))
                    {
                        return (int)char.ToUpper(keyCode[0]);
                    }
                    return 0;
            }
        }

        /// <summary>
        /// ثبت پیام در لاگ
        /// </summary>
        private void LogMessage(string message)
        {
            Debug.WriteLine($"[InputService] {message}");
            InputProcessed?.Invoke(this, message);
        }

        /// <summary>
        /// آزادسازی منابع
        /// </summary>
        public void Dispose()
        {
            DisableInput();
        }
    }

    /// <summary>
    /// کلاس کمکی برای شبیه‌سازی ورودی در سطح سیستم‌عامل
    /// </summary>
    public static class NativeInputSimulator
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        // ثابت‌های رویدادهای ماوس
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_WHEEL = 0x0800;
        private const int MOUSEEVENTF_XDOWN = 0x0080;
        private const int MOUSEEVENTF_XUP = 0x0100;

        // ثابت‌های رویدادهای کیبورد
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        /// <summary>
        /// تنظیم موقعیت مکان‌نما
        /// </summary>
        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        /// <summary>
        /// شبیه‌سازی فشردن دکمه ماوس
        /// </summary>
        public static void MouseDown(int button)
        {
            int flags;
            switch (button)
            {
                case 0: flags = MOUSEEVENTF_LEFTDOWN; break;
                case 1: flags = MOUSEEVENTF_MIDDLEDOWN; break;
                case 2: flags = MOUSEEVENTF_RIGHTDOWN; break;
                case 3: flags = MOUSEEVENTF_XDOWN; break;
                case 4: flags = MOUSEEVENTF_XDOWN; break;
                default: flags = MOUSEEVENTF_LEFTDOWN; break;
            }

            mouse_event(flags, 0, 0, button >= 3 ? button - 2 : 0, 0);
        }

        /// <summary>
        /// شبیه‌سازی رها کردن دکمه ماوس
        /// </summary>
        public static void MouseUp(int button)
        {
            int flags;
            switch (button)
            {
                case 0: flags = MOUSEEVENTF_LEFTDOWN; break;
                case 1: flags = MOUSEEVENTF_MIDDLEDOWN; break;
                case 2: flags = MOUSEEVENTF_RIGHTDOWN; break;
                case 3: flags = MOUSEEVENTF_XDOWN; break;
                case 4: flags = MOUSEEVENTF_XDOWN; break;
                default: flags = MOUSEEVENTF_LEFTDOWN; break;
            }

            mouse_event(flags, 0, 0, button >= 3 ? button - 2 : 0, 0);
        }

        /// <summary>
        /// شبیه‌سازی چرخش چرخ ماوس
        /// </summary>
        public static void MouseWheel(int delta)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, delta, 0);
        }

        /// <summary>
        /// شبیه‌سازی فشردن کلید
        /// </summary>
        public static void KeyDown(int virtualKeyCode)
        {
            keybd_event((byte)virtualKeyCode, 0, KEYEVENTF_KEYDOWN, 0);
        }

        /// <summary>
        /// شبیه‌سازی رها کردن کلید
        /// </summary>
        public static void KeyUp(int virtualKeyCode)
        {
            keybd_event((byte)virtualKeyCode, 0, KEYEVENTF_KEYUP, 0);
        }
    }
}