using System;
using System.Windows;
using System.Windows.Controls;
using RemoteDesktopClient.Models;

namespace RemoteDesktopClient.Views
{
    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml
    /// </summary>
    public partial class ConnectionDialog : Window
    {
        private bool _isJoinMode;
        
        public string SessionName { get; private set; }
        public ConnectionType ConnectionType { get; private set; }
        public string SessionId { get; private set; }
        public string AccessCode { get; private set; }
        
        /// <summary>
        /// Creates a new ConnectionDialog
        /// </summary>
        /// <param name="isJoinMode">True to show join session form, false to show create session form</param>
        public ConnectionDialog(bool isJoinMode = false)
        {
            InitializeComponent();
            
            _isJoinMode = isJoinMode;
            
            // Configure dialog based on mode
            if (_isJoinMode)
            {
                txtHeader.Text = "Join Existing Session";
                pnlCreateSession.Visibility = Visibility.Collapsed;
                pnlJoinSession.Visibility = Visibility.Visible;
            }
            else
            {
                txtHeader.Text = "Create New Session";
                pnlCreateSession.Visibility = Visibility.Visible;
                pnlJoinSession.Visibility = Visibility.Collapsed;
            }
        }
        
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (_isJoinMode)
            {
                // Validate join session form
                if (string.IsNullOrEmpty(txtSessionId.Text.Trim()))
                {
                    MessageBox.Show("Please enter a session ID", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtSessionId.Focus();
                    return;
                }
                
                if (string.IsNullOrEmpty(txtAccessCode.Text.Trim()))
                {
                    MessageBox.Show("Please enter an access code", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAccessCode.Focus();
                    return;
                }
                
                // Set properties
                SessionId = txtSessionId.Text.Trim();
                AccessCode = txtAccessCode.Text.Trim();
            }
            else
            {
                // Validate create session form
                if (string.IsNullOrEmpty(txtSessionName.Text.Trim()))
                {
                    MessageBox.Show("Please enter a session name", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtSessionName.Focus();
                    return;
                }
                
                // Set properties
                SessionName = txtSessionName.Text.Trim();
                
                // Get selected connection type
                if (cmbConnectionType.SelectedItem is ComboBoxItem selectedItem)
                {
                    string connectionTypeStr = selectedItem.Tag?.ToString();
                    
                    if (!string.IsNullOrEmpty(connectionTypeStr) && Enum.TryParse(connectionTypeStr, out ConnectionType connectionType))
                    {
                        ConnectionType = connectionType;
                    }
                    else
                    {
                        ConnectionType = ConnectionType.Controller; // Default
                    }
                }
                else
                {
                    ConnectionType = ConnectionType.Controller; // Default
                }
            }
            
            DialogResult = true;
            Close();
        }
    }
}
