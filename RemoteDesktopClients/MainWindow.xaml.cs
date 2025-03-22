using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RemoteDesktopClient.Models;
using RemoteDesktopClient.Services;
using RemoteDesktopClient.Views;
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;

namespace RemoteDesktopClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ConnectionService _connectionService;
        private readonly ScreenCaptureService _screenCaptureService;
        private readonly InputService _inputService;
        private readonly FileSystemService _fileSystemService;

        private readonly DispatcherTimer _fpsTimer;
        private int _frameCount;
        private DateTime _lastFpsUpdate;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            _connectionService = new ConnectionService();
            _screenCaptureService = new ScreenCaptureService(_connectionService);
            _inputService = new InputService(_connectionService);
            _fileSystemService = new FileSystemService(_connectionService);

            // Register event handlers
            RegisterEventHandlers();

            // Initialize FPS counter
            _fpsTimer = new DispatcherTimer();
            _fpsTimer.Interval = TimeSpan.FromSeconds(1);
            _fpsTimer.Tick += FpsTimer_Tick;
            _lastFpsUpdate = DateTime.Now;
            _frameCount = 0;

            // Set initial state
            imgRemoteScreen.Visibility = Visibility.Collapsed;
            txtNoConnection.Visibility = Visibility.Visible;

            // Start FPS timer
            _fpsTimer.Start();
        }

        #region Event Registration

        private void RegisterEventHandlers()
        {
            // Connection service events
            _connectionService.ConnectionStatusChanged += ConnectionService_ConnectionStatusChanged;
            _connectionService.MessageReceived += ConnectionService_MessageReceived;
            _connectionService.ScreenDataReceived += ConnectionService_ScreenDataReceived;
            _connectionService.ErrorOccurred += ConnectionService_ErrorOccurred;
            _connectionService.SessionInfoUpdated += ConnectionService_SessionInfoUpdated;
            _connectionService.ChatMessagesReceived += ConnectionService_ChatMessagesReceived;
            _connectionService.NotificationReceived += ConnectionService_NotificationReceived;

            // Screen capture service events
            _screenCaptureService.ScreenCaptured += ScreenCaptureService_ScreenCaptured;
            _screenCaptureService.CaptureError += ScreenCaptureService_CaptureError;

            // Input service events
            _inputService.InputStatusChanged += InputService_InputStatusChanged;

            // File system service events
            _fileSystemService.FileTransferStarted += FileSystemService_FileTransferStarted;
            _fileSystemService.FileTransferProgressChanged += FileSystemService_FileTransferProgressChanged;
            _fileSystemService.FileTransferCompleted += FileSystemService_FileTransferCompleted;
            _fileSystemService.FileTransferFailed += FileSystemService_FileTransferFailed;
        }

        #endregion

        #region Button Click Event Handlers

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string serverUrl = txtServerUrl.Text.Trim();
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter server URL, username, and password", "Connection Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update UI state to connecting
                UpdateStatus("Connecting to server...");
                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = false;

                // Try to connect
                await _connectionService.InitializeConnection(serverUrl, username, password);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Connection failed");
                btnConnect.IsEnabled = true;
            }
        }

        private async void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Disconnecting...");

                // Stop screen capture if running
                if (_screenCaptureService.IsCapturing)
                {
                    _screenCaptureService.StopCapture();
                }

                // Disconnect from server
                await _connectionService.Disconnect();

                // Clean up temp files
                _fileSystemService.CleanUpTempFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Disconnect error: {ex.Message}", "Disconnect Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStartRemoteControl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Enable input service
                _inputService.InputEnabled = true;

                // Start screen capture service if applicable
                if (_connectionService.ConnectionInfo.ConnectionType == ConnectionType.Controller &&
                    !_screenCaptureService.IsCapturing)
                {
                    _screenCaptureService.StartCapture();
                }

                // Update UI
                UpdateStatus("Remote control active");
                btnStartRemoteControl.IsEnabled = false;
                btnStopRemoteControl.IsEnabled = true;
                txtStatusRemoteControl.Text = "Active";
                txtStatusRemoteControl.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xae, 0x60)); // Green
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting remote control: {ex.Message}", "Remote Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStopRemoteControl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable input service
                _inputService.InputEnabled = false;

                // Stop screen capture if applicable
                if (_screenCaptureService.IsCapturing)
                {
                    _screenCaptureService.StopCapture();
                }

                // Update UI
                UpdateStatus("Remote control stopped");
                btnStartRemoteControl.IsEnabled = true;
                btnStopRemoteControl.IsEnabled = false;
                txtStatusRemoteControl.Text = "Inactive";
                txtStatusRemoteControl.Foreground = new SolidColorBrush(Color.FromRgb(0xe7, 0x4c, 0x3c)); // Red
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping remote control: {ex.Message}", "Remote Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open settings window
                SettingsWindow settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}", "Settings Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open about window
                AboutWindow aboutWindow = new AboutWindow();
                aboutWindow.Owner = this;
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening about window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCreateSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open session creation dialog
                ConnectionDialog dialog = new ConnectionDialog();
                dialog.Owner = this;
                dialog.Title = "Create New Session";

                if (dialog.ShowDialog() == true)
                {
                    // Create session with provided details
                    await _connectionService.CreateSession(dialog.SessionName, dialog.ConnectionType);
                    UpdateStatus($"Session created: {_connectionService.ConnectionInfo.SessionId}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating session: {ex.Message}", "Session Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnJoinSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open session join dialog
                ConnectionDialog dialog = new ConnectionDialog(true);
                dialog.Owner = this;
                dialog.Title = "Join Existing Session";

                if (dialog.ShowDialog() == true)
                {
                    // Join session with provided details
                    await _connectionService.JoinSession(dialog.SessionId, dialog.AccessCode);
                    UpdateStatus($"Joined session: {dialog.SessionId}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error joining session: {ex.Message}", "Session Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSendChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string message = txtChatMessage.Text.Trim();

                if (!string.IsNullOrEmpty(message))
                {
                    await _connectionService.SendChatMessage(message);
                    txtChatMessage.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}", "Chat Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Select File to Upload";
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == true)
                {
                    string remoteDirectory = "/"; // Default remote directory

                    // Get selected file
                    string localFilePath = dialog.FileName;

                    // Start the upload
                    UpdateStatus($"Uploading {System.IO.Path.GetFileName(localFilePath)}...");
                    prgFileTransfer.Visibility = Visibility.Visible;

                    await _fileSystemService.UploadFile(localFilePath, remoteDirectory);

                    // Refresh file list
                    await RefreshFileList(remoteDirectory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading file: {ex.Message}", "File Upload Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstFiles.SelectedItem is FileSystemItem selectedFile)
                {
                    if (selectedFile.IsDirectory)
                    {
                        MessageBox.Show("Please select a file to download", "File Download",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Show save file dialog to choose download location
                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.Title = "Save File As";
                    dialog.FileName = selectedFile.Name;

                    if (dialog.ShowDialog() == true)
                    {
                        // Start the download
                        UpdateStatus($"Downloading {selectedFile.Name}...");
                        prgFileTransfer.Visibility = Visibility.Visible;

                        string saveDirectory = System.IO.Path.GetDirectoryName(dialog.FileName);
                        await _fileSystemService.DownloadFile(selectedFile.FullPath, saveDirectory);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a file to download", "File Download",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading file: {ex.Message}", "File Download Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Prompt for folder name
                string folderName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter folder name:", "Create New Folder", "", -1, -1);

                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    string currentDirectory = "/"; // Current directory path

                    // Create the folder
                    await _fileSystemService.CreateRemoteDirectory(currentDirectory, folderName);

                    // Refresh file list
                    await RefreshFileList(currentDirectory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating folder: {ex.Message}", "Folder Creation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Remote Screen Event Handlers

        private void ImgRemoteScreen_MouseMove(object sender, MouseEventArgs e)
        {
            if (_inputService.InputEnabled)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseMove((int)position.X, (int)position.Y);
            }
        }

        private void ImgRemoteScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_inputService.InputEnabled)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseDown((int)position.X, (int)position.Y, e.ChangedButton);
            }
        }

        private void ImgRemoteScreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_inputService.InputEnabled)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseUp((int)position.X, (int)position.Y, e.ChangedButton);
            }
        }

        private void ImgRemoteScreen_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_inputService.InputEnabled)
            {
                Point position = e.GetPosition(imgRemoteScreen);
                _inputService.HandleMouseWheel((int)position.X, (int)position.Y, e.Delta);
            }
        }

        #endregion

        #region Other UI Event Handlers

        private void TxtChatMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSendChat_Click(sender, e);
            }
        }

        private async void LstFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (lstFiles.SelectedItem is FileSystemItem selectedItem)
                {
                    if (selectedItem.IsDirectory)
                    {
                        // Navigate to the selected directory
                        await RefreshFileList(selectedItem.FullPath);
                    }
                    else
                    {
                        // Prompt to download the file
                        if (MessageBox.Show($"Do you want to download {selectedItem.Name}?", "Download File",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            BtnDownloadFile_Click(sender, e);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "File Operation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SldQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtQualityValue != null)
            {
                int quality = (int)sldQuality.Value;
                txtQualityValue.Text = $"{quality}%";

                // Update screen capture quality
                _screenCaptureService.CaptureQuality = quality;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Clean up resources
            _fpsTimer.Stop();

            // Stop screen capture if running
            if (_screenCaptureService.IsCapturing)
            {
                _screenCaptureService.StopCapture();
            }

            // Disconnect if connected
            if (_connectionService.ConnectionInfo.IsConnected)
            {
                try
                {
                    _connectionService.Disconnect().Wait();
                }
                catch
                {
                    // Ignore disconnection errors during shutdown
                }
            }

            // Clean up temp files
            _fileSystemService.CleanUpTempFiles();
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            // Update FPS counter
            var elapsed = DateTime.Now - _lastFpsUpdate;
            if (elapsed.TotalSeconds >= 1)
            {
                txtStatusFps.Text = _frameCount.ToString();
                _frameCount = 0;
                _lastFpsUpdate = DateTime.Now;
            }
        }

        #endregion

        #region Service Event Handlers

        private void ConnectionService_ConnectionStatusChanged(object sender, bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    // Update UI for connected state
                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                    btnCreateSession.IsEnabled = true;
                    btnJoinSession.IsEnabled = true;

                    txtConnectionStatus.Text = "Connected";
                    txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xae, 0x60)); // Green

                    txtStatusConnection.Text = "Connected";
                    txtStatusConnection.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xae, 0x60)); // Green

                    UpdateStatus($"Connected to {_connectionService.ConnectionInfo.ServerUrl}");
                }
                else
                {
                    // Update UI for disconnected state
                    btnConnect.IsEnabled = true;
                    btnDisconnect.IsEnabled = false;
                    btnCreateSession.IsEnabled = false;
                    btnJoinSession.IsEnabled = false;
                    btnStartRemoteControl.IsEnabled = false;
                    btnStopRemoteControl.IsEnabled = false;

                    txtConnectionStatus.Text = "Disconnected";
                    txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xe7, 0x4c, 0x3c)); // Red

                    txtStatusConnection.Text = "Disconnected";
                    txtStatusConnection.Foreground = new SolidColorBrush(Color.FromRgb(0xe7, 0x4c, 0x3c)); // Red

                    txtSessionId.Text = "-";
                    txtAccessCode.Text = "-";
                    txtParticipants.Text = "-";

                    txtStatusRemoteControl.Text = "Inactive";
                    txtStatusRemoteControl.Foreground = new SolidColorBrush(Color.FromRgb(0xe7, 0x4c, 0x3c)); // Red

                    // Hide remote screen
                    imgRemoteScreen.Visibility = Visibility.Collapsed;
                    txtNoConnection.Visibility = Visibility.Visible;

                    UpdateStatus("Disconnected");
                }
            });
        }

        private void ConnectionService_MessageReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                // Add to message list
                ListBoxItem item = new ListBoxItem();
                item.Content = $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}";
                lstChatMessages.Items.Add(item);
                lstChatMessages.ScrollIntoView(item);

                UpdateStatus(message);
            });
        }

        private void ConnectionService_ScreenDataReceived(object sender, byte[] screenData)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Convert screen data to bitmap and display
                    BitmapImage bitmapImage = _screenCaptureService.ConvertByteArrayToBitmapImage(screenData);

                    if (bitmapImage != null)
                    {
                        imgRemoteScreen.Source = bitmapImage;

                        // Show remote screen if hidden
                        if (imgRemoteScreen.Visibility != Visibility.Visible)
                        {
                            imgRemoteScreen.Visibility = Visibility.Visible;
                            txtNoConnection.Visibility = Visibility.Collapsed;
                        }

                        // Update FPS counter
                        _frameCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing screen data: {ex.Message}");
                }
            });
        }

        private void ConnectionService_ErrorOccurred(object sender, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"Error: {errorMessage}");
            });
        }

        private void ConnectionService_SessionInfoUpdated(object sender, RemoteSessionInfo sessionInfo)
        {
            Dispatcher.Invoke(() =>
            {
                // Update session info UI
                txtSessionId.Text = sessionInfo.SessionId.ToString();
                txtAccessCode.Text = "********"; // Don't show the actual access code for security

                // Update participants info
                int participantCount = sessionInfo.Participants?.Count ?? 0;
                txtParticipants.Text = participantCount.ToString();

                // Enable/disable remote control based on session type
                btnStartRemoteControl.IsEnabled = true;

                UpdateStatus($"Session info updated: {sessionInfo.SessionName}");
            });
        }

        private void ConnectionService_ChatMessagesReceived(object sender, List<ChatMessage> messages)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var message in messages)
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Content = $"[{message.Timestamp.ToString("HH:mm:ss")}] {message.Username}: {message.Message}";
                    lstChatMessages.Items.Add(item);
                }

                if (messages.Count > 0)
                {
                    lstChatMessages.ScrollIntoView(lstChatMessages.Items[lstChatMessages.Items.Count - 1]);
                }
            });
        }

        private void ConnectionService_NotificationReceived(object sender, NotificationMessage notification)
        {
            Dispatcher.Invoke(() =>
            {
                // Display notification
                UpdateStatus($"{notification.Title}: {notification.Message}");
            });
        }

        private void ScreenCaptureService_ScreenCaptured(object sender, byte[] screenData)
        {
            // Frames are sent via the connection service
            // This event is mainly for debugging or additional processing
            _frameCount++;
        }

        private void ScreenCaptureService_CaptureError(object sender, Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"Screen capture error: {ex.Message}");
            });
        }

        private void InputService_InputStatusChanged(object sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"Input: {status}");
            });
        }

        private void FileSystemService_FileTransferStarted(object sender, FileTransferInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                prgFileTransfer.Value = 0;
                prgFileTransfer.Visibility = Visibility.Visible;

                UpdateStatus($"File transfer started: {e.FileName}");
            });
        }

        private void FileSystemService_FileTransferProgressChanged(object sender, FileTransferInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                prgFileTransfer.Value = e.Progress;

                UpdateStatus($"File transfer progress: {e.FileName} - {e.Progress}%");
            });
        }

        private void FileSystemService_FileTransferCompleted(object sender, FileTransferInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                prgFileTransfer.Value = 100;
                prgFileTransfer.Visibility = Visibility.Collapsed;

                UpdateStatus($"File transfer completed: {e.FileName}");
            });
        }

        private void FileSystemService_FileTransferFailed(object sender, FileTransferInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                prgFileTransfer.Visibility = Visibility.Collapsed;

                UpdateStatus($"File transfer failed: {e.FileName} - {e.ErrorMessage}");
            });
        }

        #endregion

        #region Helper Methods

        private void UpdateStatus(string message)
        {
            txtStatusMessage.Text = message;
        }

        private async Task RefreshFileList(string path)
        {
            try
            {
                // Clear current list
                lstFiles.Items.Clear();

                // Get file list from server
                var files = await _fileSystemService.GetRemoteFileList(path);

                // Add parent directory entry if not at root
                if (path != "/")
                {
                    lstFiles.Items.Add(new FileSystemItem
                    {
                        Name = "..",
                        FullPath = System.IO.Path.GetDirectoryName(path).Replace("\\", "/"),
                        IsDirectory = true,
                        Size = 0,
                        LastModified = DateTime.Now
                    });
                }

                // Add all items to list
                foreach (var file in files)
                {
                    lstFiles.Items.Add(file);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing file list: {ex.Message}", "File System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}