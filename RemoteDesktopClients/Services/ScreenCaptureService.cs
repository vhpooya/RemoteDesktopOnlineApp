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
    public class ScreenCaptureService : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _captureTask;
        private readonly ConnectionService _connectionService;
        private int _captureIntervalMs = 200;  // Default to 5 FPS
        private bool _isCapturing;
        private Rectangle _captureArea;
        private int _captureQuality = 70;  // JPEG quality (0-100)
        private readonly object _lockObject = new object();

        public int CaptureIntervalMs
        {
            get => _captureIntervalMs;
            set => _captureIntervalMs = Math.Max(50, value); // Min 50ms (20 FPS)
        }

        public int CaptureQuality
        {
            get => _captureQuality;
            set => _captureQuality = Math.Max(1, Math.Min(100, value));
        }

        public bool IsCapturing
        {
            get => _isCapturing;
            private set => _isCapturing = value;
        }

        public Rectangle CaptureArea
        {
            get => _captureArea;
            set => _captureArea = value;
        }

        public event EventHandler<byte[]> ScreenCaptured;
        public event EventHandler<Exception> CaptureError;

        public ScreenCaptureService(ConnectionService connectionService)
        {
            _connectionService = connectionService;

            // Default to full primary screen
            _captureArea = new Rectangle(0, 0,
                (int)SystemParameters.PrimaryScreenWidth,
                (int)SystemParameters.PrimaryScreenHeight);
        }

        public void StartCapture()
        {
            if (IsCapturing)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            IsCapturing = true;

            _captureTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Capture the screen
                        byte[] screenData = CaptureScreen();

                        // Notify subscribers
                        ScreenCaptured?.Invoke(this, screenData);

                        // Send to remote connection if connected
                        if (_connectionService.ConnectionInfo.IsConnected)
                        {
                            await _connectionService.SendScreenData(screenData);
                        }

                        // Wait for next capture interval
                        await Task.Delay(_captureIntervalMs, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, do nothing
                }
                catch (Exception ex)
                {
                    CaptureError?.Invoke(this, ex);
                }
                finally
                {
                    IsCapturing = false;
                }
            }, _cancellationTokenSource.Token);
        }

        public void StopCapture()
        {
            if (!IsCapturing)
                return;

            lock (_lockObject)
            {
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }

            // Wait for task to complete
            _captureTask?.Wait(1000);
            _captureTask = null;

            IsCapturing = false;
        }

        private byte[] CaptureScreen()
        {
            using (Bitmap bitmap = new Bitmap(CaptureArea.Width, CaptureArea.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        CaptureArea.Left,
                        CaptureArea.Top,
                        0,
                        0,
                        new System.Drawing.Size(CaptureArea.Width, CaptureArea.Height),
                        CopyPixelOperation.SourceCopy);
                }

                // Compress to JPEG
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    EncoderParameters encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)CaptureQuality);

                    ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                    bitmap.Save(memoryStream, jpegEncoder, encoderParams);

                    return memoryStream.ToArray();
                }
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public BitmapImage ConvertByteArrayToBitmapImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memoryStream = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Important for cross-thread usage
            }
            return bitmapImage;
        }

        public void SetCaptureAreaToScreen(int screenIndex)
        {
            // Get the screen bounds
            if (screenIndex >= 0 && screenIndex < System.Windows.Forms.Screen.AllScreens.Length)
            {
                var screen = System.Windows.Forms.Screen.AllScreens[screenIndex];
                CaptureArea = new Rectangle(
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    screen.Bounds.Width,
                    screen.Bounds.Height);
            }
            else
            {
                // Default to primary screen
                var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
                CaptureArea = new Rectangle(
                    primaryScreen.Bounds.X,
                    primaryScreen.Bounds.Y,
                    primaryScreen.Bounds.Width,
                    primaryScreen.Bounds.Height);
            }
        }

        public void Dispose()
        {
            StopCapture();
        }
    }
}