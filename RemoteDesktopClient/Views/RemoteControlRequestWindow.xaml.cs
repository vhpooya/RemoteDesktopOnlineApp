using System;
using System.Windows;

namespace RemoteDesktopClient.Views
{
    /// <summary>
    /// منطق تعاملی برای RemoteControlRequestWindow.xaml
    /// </summary>
    public partial class RemoteControlRequestWindow : Window
    {
        // شناسه اتصال کاربر درخواست‌کننده
        private string _connectionId;

        // رویدادها برای پاسخ به درخواست
        public event EventHandler<string> RequestAccepted;
        public event EventHandler<string> RequestRejected;

        public RemoteControlRequestWindow(string userName, string connectionId)
        {
            InitializeComponent();

            // ذخیره اطلاعات
            _connectionId = connectionId;

            // نمایش اطلاعات
            txtUserName.Text = userName;
            txtConnectionId.Text = connectionId;

            // تنظیم عنوان پنجره
            Title = $"درخواست کنترل از راه دور از طرف {userName}";
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            // اطلاع‌رسانی پذیرش درخواست
            RequestAccepted?.Invoke(this, _connectionId);

            // بستن پنجره
            Close();
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            // اطلاع‌رسانی رد درخواست
            RequestRejected?.Invoke(this, _connectionId);

            // بستن پنجره
            Close();
        }
    }
}