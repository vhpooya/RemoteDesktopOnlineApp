using System;
using System.Collections.Generic;

namespace RemoteDesktopClient.Models
{
    /// <summary>
    /// ��� ������� ����� ���?��
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// ���� ����
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// ����� ����� �� ��� ���?�� (�� ���� ��Ϙ�� �?��� �?����)
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// ��?� �����? (��� ����) ���? ����� �� ��� ���
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// ���?� ����� �� ����
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// ����� ����� ���?���? �� �� ��� ����� ���?�� ���
        /// </summary>
        public string RemoteSupportConnectionId { get; set; }

        /// <summary>
        /// ����� ���?��? �� �� ��� ����� �� ���?�
        /// </summary>
        public string RemoteClientId { get; set; }

        /// <summary>
        /// �?��� ���� ���?��
        /// </summary>
        public string OperatingSystem => Environment.OSVersion.ToString();

        /// <summary>
        /// ��� ���?��� ���?��
        /// </summary>
        public string MachineName => Environment.MachineName;

        /// <summary>
        /// ��� ����� ���?
        /// </summary>
        public string Username => Environment.UserName;

        /// <summary>
        /// ���� ���?� �����
        /// </summary>
        public DateTime LastConnected { get; set; }

        /// <summary>
        /// ����� ���� ���? (�� ���� ����)
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// ��� ����� (����᝘���� ?� ����������)
        /// </summary>
        public ConnectionType ConnectionType { get; set; }

        /// <summary>
        /// ���?��� ����? �����
        /// </summary>
        public ConnectionSettings Settings { get; set; }

        /// <summary>
        /// ������ �?ԝ���
        /// </summary>
        public ConnectionInfo()
        {
            // �������? ���?�
            ServerUrl = "";
            ClientId = "";
            AccessKey = "";
            IsConnected = false;
            LastConnected = DateTime.Now;
            ConnectionType = ConnectionType.Unknown;
            Settings = new ConnectionSettings();
        }
    }

    /// <summary>
    /// ��� �����
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// ������
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// ����᝘���� (�� ��� ����� ���?�� �?��)
        /// </summary>
        Controller = 1,

        /// <summary>
        /// ���������� (�� ��� ����� ��� ���� ����� �?��)
        /// </summary>
        Controlled = 2,

        /// <summary>
        /// �����坘���� (��� ������ ���� �����)
        /// </summary>
        Viewer = 3
    }

    /// <summary>
    /// ���?��� �����
    /// </summary>
    public class ConnectionSettings
    {
        /// <summary>
        /// �?� ��� ���� ��?�� ��Ͽ
        /// </summary>
        public bool SaveCredentials { get; set; }

        /// <summary>
        /// �?� ����� ��Ϙ�� �� ���� ������ ����� ��Ͽ
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// �?� ������ʝ��? ����� �� ��� ��Ϙ�� ��?���� ���Ͽ
        /// </summary>
        public bool AutoAcceptConnections { get; set; }

        /// <summary>
        /// �?� ���� ����� ��� ��� ��Ͽ
        /// </summary>
        public bool PlaySoundOnConnection { get; set; }

        /// <summary>
        /// �?� ������� ���?� ���� ���Ͽ
        /// </summary>
        public bool ShowNotifications { get; set; }

        /// <summary>
        /// ��� ������?
        /// </summary>
        public EncryptionLevel EncryptionLevel { get; set; }

        /// <summary>
        /// ������ �?ԝ���
        /// </summary>
        public ConnectionSettings()
        {
            // ����?� �?ԝ���
            SaveCredentials = false;
            AutoConnect = false;
            AutoAcceptConnections = false;
            PlaySoundOnConnection = true;
            ShowNotifications = true;
            EncryptionLevel = EncryptionLevel.High;
        }
    }

    /// <summary>
    /// ��� ������?
    /// </summary>
    public enum EncryptionLevel
    {
        /// <summary>
        /// ���� ������?
        /// </summary>
        None = 0,

        /// <summary>
        /// ������? ��?�
        /// </summary>
        Basic = 1,

        /// <summary>
        /// ������? �����
        /// </summary>
        Medium = 2,

        /// <summary>
        /// ������? ����
        /// </summary>
        High = 3
    }
}