using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace RemoteDesktopClient.Services
{
    public class InputService
    {
        private readonly ConnectionService _connectionService;
        private bool _inputEnabled = false;
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;
        private int _remoteScreenWidth = 1920;
        private int _remoteScreenHeight = 1080;

        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // Mouse event constants
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        // Keyboard event constants
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        #endregion

        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        public event EventHandler<string> InputStatusChanged;

        public InputService(ConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public void SetRemoteScreenDimensions(int width, int height)
        {
            _remoteScreenWidth = width;
            _remoteScreenHeight = height;

            // Calculate scaling factors
            _scaleX = Screen.PrimaryScreen.Bounds.Width / (double)width;
            _scaleY = Screen.PrimaryScreen.Bounds.Height / (double)height;
        }

        #region Input Handlers

        public void HandleMouseMove(int x, int y)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Scale coordinates based on resolution differences
                int scaledX = (int)(x / _scaleX);
                int scaledY = (int)(y / _scaleY);

                // Send mouse move command to remote host
                SendInputCommand("mousemove", new Dictionary<string, object>
                {
                    {"x", scaledX},
                    {"y", scaledY}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Mouse move error: {ex.Message}");
            }
        }

        public void HandleMouseClick(int x, int y, MouseButton button)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Scale coordinates based on resolution differences
                int scaledX = (int)(x / _scaleX);
                int scaledY = (int)(y / _scaleY);

                // Send mouse click command to remote host
                SendInputCommand("mouseclick", new Dictionary<string, object>
                {
                    {"x", scaledX},
                    {"y", scaledY},
                    {"button", button.ToString().ToLower()}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Mouse click error: {ex.Message}");
            }
        }

        public void HandleMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Scale coordinates based on resolution differences
                int scaledX = (int)(x / _scaleX);
                int scaledY = (int)(y / _scaleY);

                // Send mouse double click command to remote host
                SendInputCommand("mousedoubleclick", new Dictionary<string, object>
                {
                    {"x", scaledX},
                    {"y", scaledY},
                    {"button", button.ToString().ToLower()}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Mouse double click error: {ex.Message}");
            }
        }

        public void HandleMouseDown(int x, int y, MouseButton button)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Scale coordinates based on resolution differences
                int scaledX = (int)(x / _scaleX);
                int scaledY = (int)(y / _scaleY);

                // Send mouse down command to remote host
                SendInputCommand("mousedown", new Dictionary<string, object>
                {
                    {"x", scaledX},
                    {"y", scaledY},
                    {"button", button.ToString().ToLower()}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Mouse down error: {ex.Message}");
            }
        }

        public void HandleMouseUp(int x, int y, MouseButton button)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Scale coordinates based on resolution differences
                int scaledX = (int)(x / _scaleX);
                int scaledY = (int)(y / _scaleY);

                // Send mouse up command to remote host
                SendInputCommand("mouseup", new Dictionary<string, object>
                {
                    {"x", scaledX},
                    {"y", scaledY},
                    {"button", button.ToString().ToLower()}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Mouse up error: {ex.Message}");
            }
        }

        public void HandleMouseWheel(int x, int y, int delta)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Scale coordinates based on resolution differences
                int scaledX = (int)(x / _scaleX);
                int scaledY = (int)(y / _scaleY);

                // Send mouse wheel command to remote host
                SendInputCommand("mousewheel", new Dictionary<string, object>
                {
                    {"x", scaledX},
                    {"y", scaledY},
                    {"delta", delta}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Mouse wheel error: {ex.Message}");
            }
        }

        public void HandleKeyDown(Key key, ModifierKeys modifiers)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Create a command to send to the remote host
                SendInputCommand("keydown", new Dictionary<string, object>
                {
                    {"key", key.ToString()},
                    {"modifiers", modifiers.ToString()}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Key down error: {ex.Message}");
            }
        }

        public void HandleKeyUp(Key key, ModifierKeys modifiers)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Create a command to send to the remote host
                SendInputCommand("keyup", new Dictionary<string, object>
                {
                    {"key", key.ToString()},
                    {"modifiers", modifiers.ToString()}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Key up error: {ex.Message}");
            }
        }

        public void HandleKeyPress(Key key, ModifierKeys modifiers)
        {
            if (!InputEnabled)
                return;

            try
            {
                // Create a command to send to the remote host
                SendInputCommand("keypress", new Dictionary<string, object>
                {
                    {"key", key.ToString()},
                    {"modifiers", modifiers.ToString()}
                });
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Key press error: {ex.Message}");
            }
        }

        #endregion

        #region Local Input Simulation

        public void SimulateMouseMove(int x, int y)
        {
            if (!InputEnabled)
                return;

            SetCursorPos(x, y);
        }

        public void SimulateMouseClick(int x, int y, MouseButton button)
        {
            if (!InputEnabled)
                return;

            SetCursorPos(x, y);

            uint downFlag = 0;
            uint upFlag = 0;

            switch (button)
            {
                case MouseButton.Left:
                    downFlag = MOUSEEVENTF_LEFTDOWN;
                    upFlag = MOUSEEVENTF_LEFTUP;
                    break;
                case MouseButton.Right:
                    downFlag = MOUSEEVENTF_RIGHTDOWN;
                    upFlag = MOUSEEVENTF_RIGHTUP;
                    break;
                case MouseButton.Middle:
                    downFlag = MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = MOUSEEVENTF_MIDDLEUP;
                    break;
            }

            mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
            mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
        }

        public void SimulateMouseWheel(int delta)
        {
            if (!InputEnabled)
                return;

            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, UIntPtr.Zero);
        }

        public void SimulateKeyPress(byte keyCode, bool extended = false)
        {
            if (!InputEnabled)
                return;

            uint flags = KEYEVENTF_KEYDOWN;
            if (extended)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }

            keybd_event(keyCode, 0, flags, UIntPtr.Zero);
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP | (extended ? KEYEVENTF_EXTENDEDKEY : 0), UIntPtr.Zero);
        }

        #endregion

        #region Remote Input Handling

        public void ProcessRemoteInputCommand(string command, Dictionary<string, object> parameters)
        {
            if (!InputEnabled)
                return;

            try
            {
                switch (command.ToLower())
                {
                    case "mousemove":
                        if (parameters.TryGetValue("x", out object xObj) &&
                            parameters.TryGetValue("y", out object yObj))
                        {
                            int x = Convert.ToInt32(xObj);
                            int y = Convert.ToInt32(yObj);
                            SimulateMouseMove(x, y);
                        }
                        break;

                    case "mouseclick":
                        if (parameters.TryGetValue("x", out xObj) &&
                            parameters.TryGetValue("y", out yObj) &&
                            parameters.TryGetValue("button", out object buttonObj))
                        {
                            int x = Convert.ToInt32(xObj);
                            int y = Convert.ToInt32(yObj);
                            MouseButton button = ParseMouseButton(buttonObj.ToString());
                            SimulateMouseClick(x, y, button);
                        }
                        break;

                    case "mousewheel":
                        if (parameters.TryGetValue("delta", out object deltaObj))
                        {
                            int delta = Convert.ToInt32(deltaObj);
                            SimulateMouseWheel(delta);
                        }
                        break;

                    case "keypress":
                        if (parameters.TryGetValue("key", out object keyObj))
                        {
                            byte keyCode = ConvertToVirtualKey(keyObj.ToString());
                            bool extended = IsExtendedKey(keyObj.ToString());
                            SimulateKeyPress(keyCode, extended);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Error processing remote input: {ex.Message}");
            }
        }

        private MouseButton ParseMouseButton(string buttonStr)
        {
            switch (buttonStr.ToLower())
            {
                case "right":
                    return MouseButton.Right;
                case "middle":
                    return MouseButton.Middle;
                default:
                    return MouseButton.Left;
            }
        }

        private byte ConvertToVirtualKey(string keyString)
        {
            try
            {
                // Try to parse as Key enum
                if (Enum.TryParse<Key>(keyString, out Key key))
                {
                    return (byte)KeyInterop.VirtualKeyFromKey(key);
                }

                // Try to directly convert to VK code
                if (byte.TryParse(keyString, out byte vkCode))
                {
                    return vkCode;
                }
            }
            catch
            {
                // Fallback to a default key (e.g., Space)
                return (byte)KeyInterop.VirtualKeyFromKey(Key.Space);
            }

            // Default value if parsing fails
            return (byte)KeyInterop.VirtualKeyFromKey(Key.Space);
        }

        private bool IsExtendedKey(string keyString)
        {
            if (Enum.TryParse<Key>(keyString, out Key key))
            {
                // Extended keys include arrow keys, insert, delete, home, end, page up, page down
                return key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down ||
                       key == Key.Insert || key == Key.Delete || key == Key.Home || key == Key.End ||
                       key == Key.PageUp || key == Key.PageDown;
            }

            return false;
        }

        private async Task SendInputCommand(string command, Dictionary<string, object> parameters)
        {
            try
            {
                await _connectionService.SendInputCommand(command, parameters);
            }
            catch (Exception ex)
            {
                InputStatusChanged?.Invoke(this, $"Error sending input command: {ex.Message}");
            }
        }

        #endregion
    }
}