using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace RemoteDesktopClient.Services
{
    public class FileSystemService
    {
        private readonly ConnectionService _connectionService;
        private readonly HttpClient _httpClient;
        private readonly string _tempDirectory;
        private readonly List<FileTransferInfo> _activeTransfers = new List<FileTransferInfo>();

        public event EventHandler<FileTransferInfo> FileTransferStarted;
        public event EventHandler<FileTransferInfo> FileTransferProgressChanged;
        public event EventHandler<FileTransferInfo> FileTransferCompleted;
        public event EventHandler<FileTransferInfo> FileTransferFailed;

        public IReadOnlyList<FileTransferInfo> ActiveTransfers => _activeTransfers.AsReadOnly();

        public FileSystemService(ConnectionService connectionService)
        {
            _connectionService = connectionService;
            _httpClient = new HttpClient();

            // Create temp directory for file transfers
            _tempDirectory = Path.Combine(Path.GetTempPath(), "RemoteDesktopClient");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public async Task<List<FileSystemItem>> GetRemoteFileList(string path)
        {
            if (!_connectionService.ConnectionInfo.IsConnected || string.IsNullOrEmpty(_connectionService.ConnectionInfo.SessionId))
            {
                throw new InvalidOperationException("Not connected to a session");
            }

            try
            {
                // Prepare HTTP client
                string baseUrl = _connectionService.ConnectionInfo.ServerUrl.TrimEnd('/');
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", GetAuthToken());

                // Get file list from server
                var response = await _httpClient.GetAsync(
                    $"{baseUrl}/api/FileTransfer/List?sessionId={_connectionService.ConnectionInfo.SessionId}&path={Uri.EscapeDataString(path)}");

                response.EnsureSuccessStatusCode();

                var fileList = await response.Content.ReadFromJsonAsync<List<FileSystemItem>>();
                return fileList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving file list: {ex.Message}", "File System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<FileSystemItem>();
            }
        }

        public async Task UploadFile(string localFilePath, string remoteDirectory)
        {
            if (!_connectionService.ConnectionInfo.IsConnected || string.IsNullOrEmpty(_connectionService.ConnectionInfo.SessionId))
            {
                throw new InvalidOperationException("Not connected to a session");
            }

            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException("Local file not found", localFilePath);
            }

            var fileInfo = new FileInfo(localFilePath);
            var transferInfo = new FileTransferInfo
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileInfo.Name,
                SourcePath = localFilePath,
                DestinationPath = Path.Combine(remoteDirectory, fileInfo.Name),
                Size = fileInfo.Length,
                Direction = TransferDirection.Upload,
                Status = TransferStatus.Preparing,
                StartTime = DateTime.Now
            };

            _activeTransfers.Add(transferInfo);
            OnFileTransferStarted(transferInfo);

            try
            {
                string baseUrl = _connectionService.ConnectionInfo.ServerUrl.TrimEnd('/');
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", GetAuthToken());

                // Create MultipartFormDataContent
                using (var content = new MultipartFormDataContent())
                {
                    transferInfo.Status = TransferStatus.InProgress;
                    OnFileTransferProgressChanged(transferInfo);

                    // Add the file content
                    var fileContent = new StreamContent(new FileStream(localFilePath, FileMode.Open, FileAccess.Read));
                    content.Add(fileContent, "file", fileInfo.Name);

                    // Add session ID
                    content.Add(new StringContent(_connectionService.ConnectionInfo.SessionId), "sessionId");

                    // Add destination path
                    content.Add(new StringContent(remoteDirectory), "destinationPath");

                    // Upload the file
                    var response = await _httpClient.PostAsync($"{baseUrl}/api/FileTransfer/Upload", content);
                    response.EnsureSuccessStatusCode();

                    // Update transfer status
                    transferInfo.Progress = 100;
                    transferInfo.Status = TransferStatus.Completed;
                    transferInfo.EndTime = DateTime.Now;
                    OnFileTransferCompleted(transferInfo);
                }
            }
            catch (Exception ex)
            {
                transferInfo.Status = TransferStatus.Failed;
                transferInfo.ErrorMessage = ex.Message;
                OnFileTransferFailed(transferInfo);

                MessageBox.Show($"Error uploading file: {ex.Message}", "File Transfer Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DownloadFile(string remoteFilePath, string localDirectory = null)
        {
            if (!_connectionService.ConnectionInfo.IsConnected || string.IsNullOrEmpty(_connectionService.ConnectionInfo.SessionId))
            {
                throw new InvalidOperationException("Not connected to a session");
            }

            // If local directory is not specified, use default downloads folder
            if (string.IsNullOrEmpty(localDirectory))
            {
                localDirectory = GetDefaultDownloadFolder();
            }

            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            string fileName = Path.GetFileName(remoteFilePath);
            string localFilePath = Path.Combine(localDirectory, fileName);

            // Create transfer info
            var transferInfo = new FileTransferInfo
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                SourcePath = remoteFilePath,
                DestinationPath = localFilePath,
                Direction = TransferDirection.Download,
                Status = TransferStatus.Preparing,
                StartTime = DateTime.Now
            };

            _activeTransfers.Add(transferInfo);
            OnFileTransferStarted(transferInfo);

            try
            {
                string baseUrl = _connectionService.ConnectionInfo.ServerUrl.TrimEnd('/');
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", GetAuthToken());

                // Start the download
                transferInfo.Status = TransferStatus.InProgress;
                OnFileTransferProgressChanged(transferInfo);

                // Build query string
                string url = $"{baseUrl}/api/FileTransfer/Download?sessionId={_connectionService.ConnectionInfo.SessionId}&filePath={Uri.EscapeDataString(remoteFilePath)}";

                // Download the file
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Get file size from headers if available
                if (response.Content.Headers.ContentLength.HasValue)
                {
                    transferInfo.Size = response.Content.Headers.ContentLength.Value;
                }

                // Open file stream for writing
                using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var downloadStream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[8192]; // 8KB buffer
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;

                            // Update progress
                            if (transferInfo.Size > 0)
                            {
                                transferInfo.Progress = (int)((totalBytesRead * 100) / transferInfo.Size);
                            }

                            OnFileTransferProgressChanged(transferInfo);
                        }
                    }
                }

                // Update transfer status
                transferInfo.Progress = 100;
                transferInfo.Status = TransferStatus.Completed;
                transferInfo.EndTime = DateTime.Now;
                OnFileTransferCompleted(transferInfo);
            }
            catch (Exception ex)
            {
                transferInfo.Status = TransferStatus.Failed;
                transferInfo.ErrorMessage = ex.Message;
                OnFileTransferFailed(transferInfo);

                MessageBox.Show($"Error downloading file: {ex.Message}", "File Transfer Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Delete partial file if it exists
                if (File.Exists(localFilePath))
                {
                    try
                    {
                        File.Delete(localFilePath);
                    }
                    catch
                    {
                        // Ignore errors when deleting partial file
                    }
                }
            }
        }

        public async Task CreateRemoteDirectory(string parentDirectory, string newDirectoryName)
        {
            if (!_connectionService.ConnectionInfo.IsConnected || string.IsNullOrEmpty(_connectionService.ConnectionInfo.SessionId))
            {
                throw new InvalidOperationException("Not connected to a session");
            }

            try
            {
                string baseUrl = _connectionService.ConnectionInfo.ServerUrl.TrimEnd('/');
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", GetAuthToken());

                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/FileTransfer/CreateDirectory", new
                {
                    SessionId = _connectionService.ConnectionInfo.SessionId,
                    ParentDirectory = parentDirectory,
                    DirectoryName = newDirectoryName
                });

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating directory: {ex.Message}", "File System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteRemoteFile(string filePath)
        {
            if (!_connectionService.ConnectionInfo.IsConnected || string.IsNullOrEmpty(_connectionService.ConnectionInfo.SessionId))
            {
                throw new InvalidOperationException("Not connected to a session");
            }

            try
            {
                string baseUrl = _connectionService.ConnectionInfo.ServerUrl.TrimEnd('/');
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", GetAuthToken());

                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/FileTransfer/Delete", new
                {
                    SessionId = _connectionService.ConnectionInfo.SessionId,
                    Path = filePath
                });

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting file: {ex.Message}", "File System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CleanUpTempFiles()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    var di = new DirectoryInfo(_tempDirectory);
                    foreach (var file in di.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch
                        {
                            // Ignore errors when deleting temp files
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in cleanup
            }
        }

        private string GetDefaultDownloadFolder()
        {
            // Try to get the Windows Downloads folder first
            string downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            if (Directory.Exists(downloadsPath))
            {
                return downloadsPath;
            }

            // Fall back to Documents folder
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private string GetAuthToken()
        {
            return _httpClient.DefaultRequestHeaders.Authorization?.Parameter;
        }

        #region Event Handlers

        protected virtual void OnFileTransferStarted(FileTransferInfo e)
        {
            FileTransferStarted?.Invoke(this, e);
        }

        protected virtual void OnFileTransferProgressChanged(FileTransferInfo e)
        {
            FileTransferProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnFileTransferCompleted(FileTransferInfo e)
        {
            FileTransferCompleted?.Invoke(this, e);
        }

        protected virtual void OnFileTransferFailed(FileTransferInfo e)
        {
            FileTransferFailed?.Invoke(this, e);
        }

        #endregion
    }

    public class FileTransferInfo
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public long Size { get; set; }
        public int Progress { get; set; }
        public TransferDirection Direction { get; set; }
        public TransferStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; }

        public TimeSpan Duration =>
            EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;
    }

    public enum TransferDirection
    {
        Upload,
        Download
    }

    public enum TransferStatus
    {
        Preparing,
        InProgress,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public class FileSystemItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string Extension => IsDirectory ? null : Path.GetExtension(Name);
        public string Icon => IsDirectory ? "📁" : GetFileIcon(Extension);

        private string GetFileIcon(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "📄";

            switch (extension.ToLower())
            {
                case ".txt":
                case ".log":
                    return "📝";
                case ".pdf":
                    return "📕";
                case ".doc":
                case ".docx":
                    return "📘";
                case ".xls":
                case ".xlsx":
                    return "📊";
                case ".ppt":
                case ".pptx":
                    return "📑";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    return "🖼️";
                case ".mp3":
                case ".wav":
                case ".ogg":
                    return "🎵";
                case ".mp4":
                case ".avi":
                case ".mov":
                case ".wmv":
                    return "🎞️";
                case ".zip":
                case ".rar":
                case ".7z":
                    return "📦";
                case ".exe":
                    return "⚙️";
                default:
                    return "📄";
            }
        }
    }
}