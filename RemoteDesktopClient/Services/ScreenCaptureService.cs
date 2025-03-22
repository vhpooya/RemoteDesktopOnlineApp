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
    /// ”—Ê?” ÷»ÿ Ê «—”«·  ’Ê?— ’›ÕÂ ‰„«?‘
    /// </summary>
    public class ScreenCaptureService : IDisposable
    {
        // —Ê?œ«œÂ«
        public event EventHandler<byte[]> ScreenCaptured;
        public event EventHandler<Exception> CaptureError;

        // ”—Ê?” « ’«·
        private ConnectionService _connectionService;

        //  «?„— »—«? ÷»ÿ „ò——  ’Ê?—
        private Timer _captureTimer;

        // Ê÷⁄?  ÷»ÿ
        public bool IsCapturing { get; private set; }

        //  ‰Ÿ?„«  ÷»ÿ
        public int Quality { get; set; } = 70; // œ—’œ ò?›?   ’Ê?—
        public int FrameRate { get; set; } = 10; // ›—?„ œ— À«‰?Â
        public bool CaptureMouseCursor { get; set; } = true;

        // «?Ã«œ ‘?¡
        public ScreenCaptureService()
        {
            IsCapturing = false;
        }

        /// <summary>
        /// ‘—Ê⁄ ÷»ÿ  ’Ê?—
        /// </summary>
        /// <param name="connectionService">”—Ê?” « ’«· »—«? «—”«· œ«œÂ</param>
        public void StartCapture(ConnectionService connectionService)
        {
            if (IsCapturing)
                return;

            _connectionService = connectionService;
            IsCapturing = true;

            // „Õ«”»Â ›«’·Â “„«‰? »?‰ ›—?„ùÂ«
            int interval = 1000 / FrameRate;

            // ‘—Ê⁄  «?„—
            _captureTimer = new Timer(CaptureScreen, null, 0, interval);
        }

        /// <summary>
        ///  Êﬁ› ÷»ÿ  ’Ê?—
        /// </summary>
        public void StopCapture()
        {
            if (!IsCapturing)
                return;

            //  Êﬁ›  «?„—
            _captureTimer?.Dispose();
            _captureTimer = null;

            IsCapturing = false;
        }

        /// <summary>
        /// ÷»ÿ  ’Ê?— ’›ÕÂ ‰„«?‘
        /// </summary>
        private async void CaptureScreen(object state)
        {
            try
            {
                if (!IsCapturing || _connectionService == null)
                    return;

                // ÷»ÿ  ’Ê?— ’›ÕÂ «’·?
                byte[] screenData = CaptureScreenAsByteArray();

                // «—”«· —Ê?œ«œ
                ScreenCaptured?.Invoke(this, screenData);

                // «—”«· »Â ”—Ê—
                await _connectionService.SendScreenDataAsync(screenData);
            }
            catch (Exception ex)
            {
                // «⁄·«„ Œÿ«
                CaptureError?.Invoke(this, ex);

                //  Êﬁ› ÷»ÿ œ— ’Ê—  »—Ê“ Œÿ«
                StopCapture();
            }
        }

        /// <summary>
        /// ÷»ÿ  ’Ê?— ’›ÕÂ ‰„«?‘ Ê  »œ?· »Â ¬—«?Â »«? 
        /// </summary>
        private byte[] CaptureScreenAsByteArray()
        {
            using (Bitmap bitmap = CaptureScreen())
            {
                //  ‰Ÿ?„ Å«—«„ —Â«? ›‘—œÂù”«“? JPEG
                ImageCodecInfo jpegCodec = GetJpegCodecInfo();
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, Quality);

                //  »œ?· »Â ¬—«?Â »«? 
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, jpegCodec, encoderParams);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// ÷»ÿ  ’Ê?— ’›ÕÂ ‰„«?‘ «’·?
        /// </summary>
        private Bitmap CaptureScreen()
        {
            // œ—?«›  «‰œ«“Â ’›ÕÂ ‰„«?‘ «’·?
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // «?Ã«œ  ’Ê?—
            Bitmap screenBitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format24bppRgb);

            using (Graphics graphics = Graphics.FromImage(screenBitmap))
            {
                // òÅ? ’›ÕÂ ‰„«?‘
                graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);

                // ÷»ÿ ‰‘«‰ê— „«Ê”
                if (CaptureMouseCursor)
                {
                    CaptureCursor(graphics);
                }
            }

            return screenBitmap;
        }

        /// <summary>
        /// «›“Êœ‰ ‰‘«‰ê— „«Ê” »Â  ’Ê?—
        /// </summary>
        private void CaptureCursor(Graphics g)
        {
            // «” ›«œÂ «“ API Â«? Ê?‰œÊ“ »—«? œ—?«›  „ò«‰ „«Ê” Ê ¬?òÊ‰ ›⁄·? ¬‰
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
                        // „Õ«”»Â „ò«‰ Ê«ﬁ⁄? ‰ﬁÿÂ Â« ù«”Å«  ‰‘«‰ê—
                        System.Drawing.Point hotSpot = new System.Drawing.Point(iconInfo.xHotspot, iconInfo.yHotspot);
                        System.Drawing.Point cursorLocation = new System.Drawing.Point(cursorInfo.ptScreenPos.x - hotSpot.X, cursorInfo.ptScreenPos.y - hotSpot.Y);

                        // —”„ ¬?òÊ‰
                        using (Icon icon = Icon.FromHandle(hicon))
                        {
                            g.DrawIcon(icon, cursorLocation.X, cursorLocation.Y);
                        }

                        // ¬“«œ”«“? „‰«»⁄
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
        /// œ—?«›  «ÿ·«⁄«  òÊœò JPEG
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
        ///  »œ?· ¬—«?Â »«?  »Â  ’Ê?— ﬁ«»· ‰„«?‘ œ— WPF
        /// </summary>
        public BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(byteArray);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // »—«? «” ›«œÂ œ— thread Â«? „Œ ·›

            return bitmapImage;
        }

        /// <summary>
        /// ¬“«œ”«“? „‰«»⁄
        /// </summary>
        public void Dispose()
        {
            StopCapture();
        }

        #region  Ê«»⁄ API Ê?‰œÊ“ »—«? ÷»ÿ ‰‘«‰ê— „«Ê”

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