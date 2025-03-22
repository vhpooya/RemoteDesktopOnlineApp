using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace RemoteDesktopClient.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            
            // Load settings
            LoadSettings();
            
            // Set up event handlers
            sldQuality.ValueChanged += SldQuality_ValueChanged;
            sldFrameRate.ValueChanged += SldFrameRate_ValueChanged;
            
            // Populate screens dropdown
            PopulateScreensComboBox();
        }

        private void PopulateScreensComboBox()
        {
            // Add screens to combo box
            cmbCaptureScreen.Items.Clear();
            
            // Add "Primary Screen" option
            cmbCaptureScreen.Items.Add(new ComboBoxItem { Content = "Primary Screen", Tag = -1 });
            
            // Add all available screens
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen screen = Screen.AllScreens[i];
                bool isPrimary = screen.Primary;
                
                cmbCaptureScreen.Items.Add(new ComboBoxItem 
                { 
                    Content = $"Screen {i + 1} ({screen.Bounds.Width}x{screen.Bounds.Height}){(isPrimary ? " - Primary" : "")}", 
                    Tag = i 
                });
            }
            
            // Select the first item by default
            if (cmbCaptureScreen.Items.Count > 0)
            {
                cmbCaptureScreen.SelectedIndex = 0;
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Load settings from app config
                var settings = Properties.Settings.Default;
                
                // Set UI controls based on settings
                sldQuality.Value = settings.CaptureQuality;
                sldFrameRate.Value = settings.FrameRate;
                chkEnableInputControl.IsChecked = settings.EnableInputControl;
                txtDefaultServerUrl.Text = settings.DefaultServerUrl;
                chkRememberCredentials.IsChecked = settings.RememberCredentials;
                chkAutoReconnect.IsChecked = settings.AutoReconnect;
                chkEncryption.IsChecked = settings.EnableEncryption;
                chkClearCache.IsChecked = settings.ClearCacheOnExit;
                
                // Set selected screen
                int screenIndex = settings.CaptureScreenIndex;
                if (screenIndex >= -1 && screenIndex < Screen.AllScreens.Length)
                {
                    // Select the corresponding screen
                    foreach (ComboBoxItem item in cmbCaptureScreen.Items)
                    {
                        if (item.Tag is int index && index == screenIndex)
                        {
                            cmbCaptureScreen.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading settings: {ex.Message}", "Settings Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Get settings from UI controls
                var settings = Properties.Settings.Default;
                
                settings.CaptureQuality = (int)sldQuality.Value;
                settings.FrameRate = (int)sldFrameRate.Value;
                settings.EnableInputControl = chkEnableInputControl.IsChecked ?? true;
                settings.DefaultServerUrl = txtDefaultServerUrl.Text.Trim();
                settings.RememberCredentials = chkRememberCredentials.IsChecked ?? false;
                settings.AutoReconnect = chkAutoReconnect.IsChecked ?? true;
                settings.EnableEncryption = chkEncryption.IsChecked ?? true;
                settings.ClearCacheOnExit = chkClearCache.IsChecked ?? true;
                
                // Get selected screen
                if (cmbCaptureScreen.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is int screenIndex)
                {
                    settings.CaptureScreenIndex = screenIndex;
                }
                else
                {
                    settings.CaptureScreenIndex = -1; // Default to primary screen
                }
                
                // Save settings
                settings.Save();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Settings Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SldQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtQualityValue != null)
            {
                int quality = (int)sldQuality.Value;
                txtQualityValue.Text = $"{quality}%";
            }
        }

        private void SldFrameRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtFrameRateValue != null)
            {
                int frameRate = (int)sldFrameRate.Value;
                txtFrameRateValue.Text = $"{frameRate} FPS";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }
    }
}
