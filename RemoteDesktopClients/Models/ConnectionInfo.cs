using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RemoteDesktopClient.Models
{
    public class ConnectionInfo : INotifyPropertyChanged
    {
        private string _serverUrl;
        private string _sessionId;
        private string _accessCode;
        private string _username;
        private bool _isConnected;
        private DateTime _connectionStartTime;
        private ConnectionType _connectionType;

        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                if (_serverUrl != value)
                {
                    _serverUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SessionId
        {
            get => _sessionId;
            set
            {
                if (_sessionId != value)
                {
                    _sessionId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AccessCode
        {
            get => _accessCode;
            set
            {
                if (_accessCode != value)
                {
                    _accessCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    // Also update connection time when connected
                    if (value)
                    {
                        ConnectionStartTime = DateTime.Now;
                    }
                }
            }
        }

        public DateTime ConnectionStartTime
        {
            get => _connectionStartTime;
            set
            {
                if (_connectionStartTime != value)
                {
                    _connectionStartTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConnectionDuration));
                }
            }
        }

        public TimeSpan ConnectionDuration => IsConnected
            ? DateTime.Now - ConnectionStartTime
            : TimeSpan.Zero;

        public ConnectionType ConnectionType
        {
            get => _connectionType;
            set
            {
                if (_connectionType != value)
                {
                    _connectionType = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum ConnectionType
    {
        Viewer,
        Controller,
        FileTransfer,
        Conference
    }
}