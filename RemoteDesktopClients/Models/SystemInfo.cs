using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RemoteDesktopClient.Models
{
    public class SystemInfo : INotifyPropertyChanged
    {
        private string _machineName;
        private string _osVersion;
        private string _currentUser;
        private string _ipAddress;
        private double _screenWidth;
        private double _screenHeight;
        private double _screenCount;
        private string _processorInfo;
        private double _memoryUsage;
        private double _totalMemory;
        private double _cpuUsage;
        private double _networkUsage;

        public string MachineName
        {
            get => _machineName;
            set
            {
                if (_machineName != value)
                {
                    _machineName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OSVersion
        {
            get => _osVersion;
            set
            {
                if (_osVersion != value)
                {
                    _osVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser != value)
                {
                    _currentUser = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ScreenWidth
        {
            get => _screenWidth;
            set
            {
                if (_screenWidth != value)
                {
                    _screenWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ScreenHeight
        {
            get => _screenHeight;
            set
            {
                if (_screenHeight != value)
                {
                    _screenHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ScreenCount
        {
            get => _screenCount;
            set
            {
                if (_screenCount != value)
                {
                    _screenCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProcessorInfo
        {
            get => _processorInfo;
            set
            {
                if (_processorInfo != value)
                {
                    _processorInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public double MemoryUsage
        {
            get => _memoryUsage;
            set
            {
                if (Math.Abs(_memoryUsage - value) > 0.01)
                {
                    _memoryUsage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MemoryUsagePercentage));
                }
            }
        }

        public double TotalMemory
        {
            get => _totalMemory;
            set
            {
                if (Math.Abs(_totalMemory - value) > 0.01)
                {
                    _totalMemory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MemoryUsagePercentage));
                }
            }
        }

        public double MemoryUsagePercentage => TotalMemory > 0 ? (MemoryUsage / TotalMemory) * 100 : 0;

        public double CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (Math.Abs(_cpuUsage - value) > 0.1)
                {
                    _cpuUsage = value;
                    OnPropertyChanged();
                }
            }
        }

        public double NetworkUsage
        {
            get => _networkUsage;
            set
            {
                if (Math.Abs(_networkUsage - value) > 0.1)
                {
                    _networkUsage = value;
                    OnPropertyChanged();
                }
            }
        }

        public SystemInfo()
        {
            // Initialize with system information
            InitializeSystemInfo();
        }

        private void InitializeSystemInfo()
        {
            try
            {
                MachineName = Environment.MachineName;
                OSVersion = Environment.OSVersion.ToString();
                CurrentUser = Environment.UserName;

                // These would use more specific APIs in a real implementation
                ScreenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                ScreenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                ScreenCount = System.Windows.Forms.Screen.AllScreens.Length;

                // More detailed information would require platform-specific APIs
                ProcessorInfo = Environment.ProcessorCount + " cores";

                // Real implementation would use performance counters
                UpdateSystemMetrics();
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error initializing system info: {ex.Message}");
            }
        }

        public void UpdateSystemMetrics()
        {
            // In a real implementation, this would use performance counters
            // or WMI to get real-time metrics

            // This is just placeholder implementation
            var random = new Random();
            CpuUsage = random.Next(5, 95);

            // Simulate memory usage (GB)
            TotalMemory = 16.0;  // 16 GB total
            MemoryUsage = random.Next(2, 14);  // 2-14 GB used

            // Network usage in Mbps
            NetworkUsage = random.Next(1, 100);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}