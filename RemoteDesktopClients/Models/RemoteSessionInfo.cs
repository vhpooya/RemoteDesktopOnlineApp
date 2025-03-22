using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RemoteDesktopClient.Models
{
    public class RemoteSessionInfo : INotifyPropertyChanged
    {
        private int _sessionId;
        private string _sessionName;
        private DateTime _startTime;
        private TimeSpan _duration;
        private string _hostName;
        private string _clientName;
        private SessionStatus _status;
        private List<SessionParticipant> _participants;
        private bool _isRecording;
        private int _fileTransferCount;
        private string _connectionQuality;

        public int SessionId
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

        public string SessionName
        {
            get => _sessionName;
            set
            {
                if (_sessionName != value)
                {
                    _sessionName = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HostName
        {
            get => _hostName;
            set
            {
                if (_hostName != value)
                {
                    _hostName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ClientName
        {
            get => _clientName;
            set
            {
                if (_clientName != value)
                {
                    _clientName = value;
                    OnPropertyChanged();
                }
            }
        }

        public SessionStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<SessionParticipant> Participants
        {
            get => _participants;
            set
            {
                if (_participants != value)
                {
                    _participants = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    OnPropertyChanged();
                }
            }
        }

        public int FileTransferCount
        {
            get => _fileTransferCount;
            set
            {
                if (_fileTransferCount != value)
                {
                    _fileTransferCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConnectionQuality
        {
            get => _connectionQuality;
            set
            {
                if (_connectionQuality != value)
                {
                    _connectionQuality = value;
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

    public class SessionParticipant
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public bool HasControlAccess { get; set; }
        public bool IsConnected { get; set; }
        public DateTime JoinTime { get; set; }
    }

    public enum SessionStatus
    {
        Connecting,
        Connected,
        Disconnected,
        Failed,
        Waiting,
        Ended
    }
}