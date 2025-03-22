using System;
using System.Windows;

namespace RemoteDesktopClient.Views
{
    /// <summary>
    /// منطق تعاملی برای ConnectToRemoteWindow.xaml
    /// </summary>
    public partial class ConnectToRemoteWindow : Window
    {
        // اطلاعات اتصال
        public string RemoteClientId { get; private set; }
        public string AccessKey { get; private set; }
        public bool SaveCredentials { get; private set; }

        public ConnectToRemoteWindow()
        {
            InitializeComponent();

            // تنظیم فوکوس اولیه
            Loaded += (s, e) => txtRemoteId.Focus();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            // بررسی معتبر بودن فیلدها
            if (string.IsNullOrWhiteSpace(txtRemoteId.Text))
            {
                MessageBox.Show("لطفاً شناسه کلاینت راه دور را وارد کنید", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRemoteId.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAccessKey.Password))
            {
                MessageBox.Show("لطفاً رمز دسترسی را وارد کنید", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAccessKey.Focus();
                return;
            }

            // ذخیره اطلاعات
            RemoteClientId = txtRemoteId.Text.Trim();
            AccessKey = txtAccessKey.Password;
            SaveCredentials = chkSaveCredentials.IsChecked ?? false;

            // بستن پنجره با نتیجه موفق
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // بستن پنجره با نتیجه ناموفق
            DialogResult = false;
            Close();
        }
    }
}