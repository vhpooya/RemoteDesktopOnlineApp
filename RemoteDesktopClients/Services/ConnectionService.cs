using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteDesktopClient.Models;
using RemoteDesktopClient.Helpers;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace RemoteDesktopClient.Services
{
    public class ConnectionService : IDisposable
    {
        private HubConnection _remoteSessionHub;
        private HubConnection _chatHub;
        private HubConnection _notificationHub;
        private readonly HttpClient _httpClient;

        public ConnectionInfo ConnectionInfo { get; private set; }
        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<string> MessageReceived;
        public event EventHandler<byte[]> ScreenDataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<RemoteSessionInfo> SessionInfoUpdated;
        public event EventHandler<List<ChatMessage>> ChatMessagesReceived;
        public event EventHandler<NotificationMessage> NotificationReceived;

        public ConnectionService()
        {
            ConnectionInfo = new ConnectionInfo();
            _httpClient = new HttpClient();
        }

        public async Task InitializeConnection(string serverUrl, string username, string password)
        {
            try
            {
                // Set base URL for HTTP client
                _httpClient.BaseAddress = new Uri(serverUrl);

                // Authenticate user first
                var authResult = await AuthenticateUser(username, password);
                if (!authResult)
                {
                    OnErrorOccurred("Authentication failed. Please check your credentials.");
                    return;
                }

                // Store connection info
                ConnectionInfo.ServerUrl = serverUrl;
                ConnectionInfo.Username = username;

                // Initialize SignalR hub connections
                await InitializeHubConnections();

                // Set up event handlers for all hubs
                RegisterHubEventHandlers();

                // Start hub connections
                await StartHubConnections();

                // Update connection status
                ConnectionInfo.IsConnected = true;
                OnConnectionStatusChanged(true);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to initialize connection: {ex.Message}");
                ConnectionInfo.IsConnected = false;
                OnConnectionStatusChanged(false);
            }
        }

        public async Task JoinSession(string sessionId, string accessCode)
        {
            try
            {
                if (!ConnectionInfo.IsConnected)
                {
                    OnErrorOccurred("Not connected to server. Please connect first.");
                    return;
                }

                ConnectionInfo.SessionId = sessionId;
                ConnectionInfo.AccessCode = accessCode;

                // Join the remote session via hub
                await _remoteSessionHub.InvokeAsync("JoinSession", sessionId, accessCode);

                // Get initial session information
                await GetSessionInfo(sessionId);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to join session: {ex.Message}");
            }
        }

        public async Task CreateSession(string sessionName, ConnectionType connectionType)
        {
            try
            {
                if (!ConnectionInfo.IsConnected)
                {
                    OnErrorOccurred("Not connected to server. Please connect first.");
                    return;
                }

                ConnectionInfo.ConnectionType = connectionType;

                // Create a new session via HTTP API
                var response = await _httpClient.PostAsJsonAsync("api/RemoteSession/Create", new
                {
                    SessionName = sessionName,
                    ConnectionType = connectionType.ToString()
                });

                if (response.IsSuccessStatusCode)
                {
                    var sessionData = await response.Content.ReadFromJsonAsync<SessionCreationResponse>();
                    ConnectionInfo.SessionId = sessionData.SessionId;
                    ConnectionInfo.AccessCode = sessionData.AccessCode;

                    // Join the created session
                    await _remoteSessionHub.InvokeAsync("JoinSession", sessionData.SessionId, sessionData.AccessCode);

                    // Get initial session information
                    await GetSessionInfo(sessionData.SessionId);
                }
                else
                {
                    OnErrorOccurred($"Failed to create session: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to create session: {ex.Message}");
            }
        }

        public async Task DisconnectSession()
        {
            try
            {
                if (!string.IsNullOrEmpty(ConnectionInfo.SessionId))
                {
                    await _remoteSessionHub.InvokeAsync("LeaveSession", ConnectionInfo.SessionId);
                    ConnectionInfo.SessionId = null;
                    ConnectionInfo.AccessCode = null;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error during session disconnect: {ex.Message}");
            }
        }

        public async Task Disconnect()
        {
            try
            {
                // Disconnect from session if active
                await DisconnectSession();

                // Stop all hub connections
                await StopHubConnections();

                // Update connection state
                ConnectionInfo.IsConnected = false;
                OnConnectionStatusChanged(false);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error during disconnect: {ex.Message}");
            }
        }

        public async Task SendChatMessage(string message)
        {
            try
            {
                if (_chatHub.State == HubConnectionState.Connected && !string.IsNullOrEmpty(ConnectionInfo.SessionId))
                {
                    await _chatHub.InvokeAsync("SendMessage", ConnectionInfo.SessionId, message);
                }
                else
                {
                    OnErrorOccurred("Chat connection is not available.");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to send message: {ex.Message}");
            }
        }

        public async Task SendScreenData(byte[] screenData)
        {
            try
            {
                if (_remoteSessionHub.State == HubConnectionState.Connected && !string.IsNullOrEmpty(ConnectionInfo.SessionId))
                {
                    await _remoteSessionHub.InvokeAsync("SendScreenData", ConnectionInfo.SessionId, screenData);
                }
                else
                {
                    OnErrorOccurred("Remote session connection is not available.");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to send screen data: {ex.Message}");
            }
        }

        public async Task SendInputCommand(string command, Dictionary<string, object> parameters)
        {
            try
            {
                if (_remoteSessionHub.State == HubConnectionState.Connected && !string.IsNullOrEmpty(ConnectionInfo.SessionId))
                {
                    await _remoteSessionHub.InvokeAsync("SendInputCommand", ConnectionInfo.SessionId, command, parameters);
                }
                else
                {
                    OnErrorOccurred("Remote session connection is not available.");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to send input command: {ex.Message}");
            }
        }

        #region Private Methods

        private async Task<bool> AuthenticateUser(string username, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Account/Login", new
                {
                    Username = username,
                    Password = password
                });

                if (response.IsSuccessStatusCode)
                {
                    var authResult = await response.Content.ReadFromJsonAsync<AuthenticationResult>();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.Token);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Authentication error: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeHubConnections()
        {
            string baseUrl = ConnectionInfo.ServerUrl.TrimEnd('/');

            // Initialize RemoteSession hub
            _remoteSessionHub = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/remoteSessionHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_httpClient.DefaultRequestHeaders.Authorization?.Parameter);
                })
                .WithAutomaticReconnect()
                .Build();

            // Initialize Chat hub
            _chatHub = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/chatHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_httpClient.DefaultRequestHeaders.Authorization?.Parameter);
                })
                .WithAutomaticReconnect()
                .Build();

            // Initialize Notification hub
            _notificationHub = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/notificationHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_httpClient.DefaultRequestHeaders.Authorization?.Parameter);
                })
                .WithAutomaticReconnect()
                .Build();
        }

        private void RegisterHubEventHandlers()
        {
            // RemoteSession hub events
            _remoteSessionHub.On<byte[]>("ReceiveScreenData", (screenData) =>
            {
                ScreenDataReceived?.Invoke(this, screenData);
            });

            _remoteSessionHub.On<string, Dictionary<string, object>>("ReceiveInputCommand", (command, parameters) =>
            {
                // Handle input commands from remote user
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Process input command here
                    // This will be implemented in the InputService
                });
            });

            _remoteSessionHub.On<string>("ReceiveSessionMessage", (message) =>
            {
                MessageReceived?.Invoke(this, message);
            });

            _remoteSessionHub.On<string>("SessionError", (error) =>
            {
                OnErrorOccurred(error);
            });

            _remoteSessionHub.On<string, string>("ParticipantJoined", (sessionId, username) =>
            {
                MessageReceived?.Invoke(this, $"{username} joined the session.");
                // Update session info
                GetSessionInfo(sessionId).ConfigureAwait(false);
            });

            _remoteSessionHub.On<string, string>("ParticipantLeft", (sessionId, username) =>
            {
                MessageReceived?.Invoke(this, $"{username} left the session.");
                // Update session info
                GetSessionInfo(sessionId).ConfigureAwait(false);
            });

            // Chat hub events
            _chatHub.On<List<ChatMessage>>("ReceiveMessages", (messages) =>
            {
                ChatMessagesReceived?.Invoke(this, messages);
            });

            // Notification hub events
            _notificationHub.On<NotificationMessage>("ReceiveNotification", (notification) =>
            {
                NotificationReceived?.Invoke(this, notification);
            });

            // Connection events for all hubs
            _remoteSessionHub.Closed += async (error) =>
            {
                MessageReceived?.Invoke(this, "Remote session connection closed.");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _remoteSessionHub.StartAsync();
            };

            _chatHub.Closed += async (error) =>
            {
                MessageReceived?.Invoke(this, "Chat connection closed.");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _chatHub.StartAsync();
            };

            _notificationHub.Closed += async (error) =>
            {
                MessageReceived?.Invoke(this, "Notification connection closed.");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _notificationHub.StartAsync();
            };
        }

        private async Task StartHubConnections()
        {
            await _remoteSessionHub.StartAsync();
            await _chatHub.StartAsync();
            await _notificationHub.StartAsync();
        }

        private async Task StopHubConnections()
        {
            await _remoteSessionHub.StopAsync();
            await _chatHub.StopAsync();
            await _notificationHub.StopAsync();
        }

        private async Task GetSessionInfo(string sessionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/RemoteSession/Info/{sessionId}");
                if (response.IsSuccessStatusCode)
                {
                    var sessionInfo = await response.Content.ReadFromJsonAsync<RemoteSessionInfo>();
                    SessionInfoUpdated?.Invoke(this, sessionInfo);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to get session info: {ex.Message}");
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            ConnectionStatusChanged?.Invoke(this, isConnected);
        }

        private void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        public void Dispose()
        {
            // Dispose of managed resources
            DisconnectSession().Wait();
            Disconnect().Wait();
            _httpClient.Dispose();
        }

        #endregion
    }

    // Helper classes for responses
    public class SessionCreationResponse
    {
        public string SessionId { get; set; }
        public string AccessCode { get; set; }
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
    }

    public class ChatMessage
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class NotificationMessage
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
}