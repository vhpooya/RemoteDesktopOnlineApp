using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RemoteDesktopClient.Services
{
    /// <summary>
    /// ���?� ��� � ����� ���?� ���� ���?�
    /// </summary>
    public class ScreenCaptureService : IDisposable
    {
        // ��?�����
        public event EventHandler<byte[]> ScreenCaptured;
        public event EventHandler<Exception> CaptureError;

        // ���?� �����
        private ConnectionService _connectionService;

        // ��?�� ���? ��� ��� ���?�
        private Timer _captureTimer;

        // ���?� ���
        public bool IsCapturing { get; private set; }

        // ���?��� ���
        public int Quality { get; set; } = 70; // ���� �?�?� ���?�
        public int FrameRate { get; set; } = 10; // ��?� �� ���?�
        public bool CaptureMouseCursor { get; set; } = true;

        // �?��� �?�
        public ScreenCaptureService()
        {
            IsCapturing = false;
        }

        /// <summary>
        /// ���� ��� ���?�
        /// </summary>
        /// <param name="connectionService">���?� ����� ���? ����� ����</param>
        public void StartCapture(ConnectionService connectionService)
        {
            if (IsCapturing)
                return;

            _connectionService = connectionService;
            IsCapturing = true;

            // ������ ����� ����? �?� ��?���
            int interval = 1000 / FrameRate;

            // ���� ��?��
            _captureTimer = new Timer(CaptureScreen, null, 0, interval);
        }

        /// <summary>
        /// ���� ��� ���?�
        /// </summary>
        public void StopCapture()
        {
            if (!IsCapturing)
                return;

            // ���� ��?��
            _captureTimer?.Dispose();
            _captureTimer = null;

            IsCapturing = false;
        }

        /// <summary>
        /// ��� ���?� ���� ���?�
        /// </summary>
        private async void CaptureScreen(object state)
        {
            try
            {
                if (!IsCapturing || _connectionService == null)
                    return;

                // ��� ���?� ���� ���?
                byte[] screenData = CaptureScreenAsByteArray();

                // ����� ��?���
                ScreenCaptured?.Invoke(this, screenData);

                // ����� �� ����
                await _connectionService.SendScreenDataAsync(screenData);
            }
            catch (Exception ex)
            {
                // ����� ���
                CaptureError?.Invoke(this, ex);

                // ���� ��� �� ���� ���� ���
                StopCapture();
            }
        }

        /// <summary>
        /// ��� ���?� ���� ���?� � ���?� �� ���?� ��?�
        /// </summary>
        private byte[] CaptureScreenAsByteArray()
        {
            using (Bitmap bitmap = CaptureScreen())
            {
                // ���?� ���������? ��������? JPEG
                ImageCodecInfo jpegCodec = GetJpegCodecInfo();
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, Quality);

                // ���?� �� ���?� ��?�
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, jpegCodec, encoderParams);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// ��� ���?� ���� ���?� ���?
        /// </summary>
        private Bitmap CaptureScreen()
        {
            // ��?��� ������ ���� ���?� ���?
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // �?��� ���?�
            Bitmap screenBitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format24bppRgb);

            using (Graphics graphics = Graphics.FromImage(screenBitmap))
            {
                // ��? ���� ���?�
                graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);

                // ��� ����� ����
                if (CaptureMouseCursor)
                {
                    CaptureCursor(graphics);
                }
            }

            return screenBitmap;
        }

        /// <summary>
        /// ������ ����� ���� �� ���?�
        /// </summary>
        private void CaptureCursor(Graphics g)
        {
            // ������� �� API ��? �?���� ���? ��?��� ��� ���� � �?��� ���? ��
            CURSORINFO cursorInfo = new CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

            if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
            {
                IntPtr hicon = CopyIcon(cursorInfo.hCursor);
                if (hicon != IntPtr.Zero)
                {
                    ICONINFO iconInfo;
                    if (GetIconInfo(hicon, out iconInfo))
                    {
                        // ������ ��� ����? ���� ��ʝ�Ӂ�� �����
                        System.Drawing.Point hotSpot = new System.Drawing.Point(iconInfo.xHotspot, iconInfo.yHotspot);
                        System.Drawing.Point cursorLocation = new System.Drawing.Point(cursorInfo.ptScreenPos.x - hotSpot.X, cursorInfo.ptScreenPos.y - hotSpot.Y);

                        // ��� �?���
                        using (Icon icon = Icon.FromHandle(hicon))
                        {
                            g.DrawIcon(icon, cursorLocation.X, cursorLocation.Y);
                        }

                        // �������? �����
                        if (iconInfo.hbmColor != IntPtr.Zero)
                            DeleteObject(iconInfo.hbmColor);

                        if (iconInfo.hbmMask != IntPtr.Zero)
                            DeleteObject(iconInfo.hbmMask);
                    }

                    DestroyIcon(hicon);
                }
            }
        }

        /// <summary>
        /// ��?��� ������� ��Ϙ JPEG
        /// </summary>
        private ImageCodecInfo GetJpegCodecInfo()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.MimeType == "image/jpeg")
                {
                    return codec;
                }
            }

            throw new Exception("JPEG codec not found");
        }

        /// <summary>
        /// ���?� ���?� ��?� �� ���?� ���� ���?� �� WPF
        /// </summary>
        public BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(byteArray);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // ���? ������� �� thread ��? �����

            return bitmapImage;
        }

        /// <summary>
        /// �������? �����
        /// </summary>
        public void Dispose()
        {
            StopCapture();
        }

        #region ����� API �?���� ���? ��� ����� ����

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private const int CURSOR_SHOWING = 0x00000001;

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion
    }
}