using System;
using System.Collections.Generic;

namespace RemoteDesktopClient.Models
{
    /// <summary>
    /// „œ· «ÿ·«⁄«  « ’«· ò·«?‰ 
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// ¬œ—” ”—Ê—
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// ‘‰«”Â „‰Õ’— »Â ›—œ ò·«?‰  (»Â ’Ê—  ŒÊœò«— «?Ã«œ „?ù‘Êœ)
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// ò·?œ œ” —”? (—„“ Ê—Êœ) »—«? ò‰ —· «“ —«Â œÊ—
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Ê÷⁄?  « ’«· »Â ”—Ê—
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// ‘‰«”Â ò«—»— Å‘ ?»«‰? òÂ œ— Õ«· ò‰ —· ò·«?‰  «” 
        /// </summary>
        public string RemoteSupportConnectionId { get; set; }

        /// <summary>
        /// ‘‰«”Â ò·«?‰ ? òÂ œ— Õ«· ò‰ —· ¬‰ Â” ?„
        /// </summary>
        public string RemoteClientId { get; set; }

        /// <summary>
        /// ”?” „ ⁄«„· ò·«?‰ 
        /// </summary>
        public string OperatingSystem => Environment.OSVersion.ToString();

        /// <summary>
        /// ‰«„ ò«„Å?Ê — ò·«?‰ 
        /// </summary>
        public string MachineName => Environment.MachineName;

        /// <summary>
        /// ‰«„ ò«—»— Ã«—?
        /// </summary>
        public string Username => Environment.UserName;

        /// <summary>
        /// “„«‰ ¬Œ—?‰ « ’«·
        /// </summary>
        public DateTime LastConnected { get; set; }

        /// <summary>
        /// ‘‰«”Â Ã·”Â ›⁄·? (œ— ’Ê—  ÊÃÊœ)
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// ‰Ê⁄ « ’«· (ò‰ —·ùò‰‰œÂ ?« ò‰ —·ù‘Ê‰œÂ)
        /// </summary>
        public ConnectionType ConnectionType { get; set; }

        /// <summary>
        ///  ‰Ÿ?„«  «÷«›? « ’«·
        /// </summary>
        public ConnectionSettings Settings { get; set; }

        /// <summary>
        /// ”«“‰œÂ Å?‘ù›—÷
        /// </summary>
        public ConnectionInfo()
        {
            // „ﬁœ«—œÂ? «Ê·?Â
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
    /// ‰Ê⁄ « ’«·
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// ‰«„‘Œ’
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// ò‰ —·ùò‰‰œÂ (œ— Õ«· ò‰ —· ò·«?‰  œ?ê—)
        /// </summary>
        Controller = 1,

        /// <summary>
        /// ò‰ —·ù‘Ê‰œÂ (œ— Õ«· ò‰ —· ‘œ‰  Ê”ÿ ò«—»— œ?ê—)
        /// </summary>
        Controlled = 2,

        /// <summary>
        /// „‘«ÂœÂùò‰‰œÂ (›ﬁÿ „‘«ÂœÂ »œÊ‰ ò‰ —·)
        /// </summary>
        Viewer = 3
    }

    /// <summary>
    ///  ‰Ÿ?„«  « ’«·
    /// </summary>
    public class ConnectionSettings
    {
        /// <summary>
        /// ¬?« —„“ ⁄»Ê— –Œ?—Â ‘Êœø
        /// </summary>
        public bool SaveCredentials { get; set; }

        /// <summary>
        /// ¬?« « ’«· ŒÊœò«— œ— ‘—Ê⁄ »—‰«„Â «‰Ã«„ ‘Êœø
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// ¬?« œ—ŒÊ«” ùÂ«? « ’«· »Â ÿÊ— ŒÊœò«— Å–?—› Â ‘Ê‰œø
        /// </summary>
        public bool AutoAcceptConnections { get; set; }

        /// <summary>
        /// ¬?« Â‰ê«„ « ’«· ’œ« ÅŒ‘ ‘Êœø
        /// </summary>
        public bool PlaySoundOnConnection { get; set; }

        /// <summary>
        /// ¬?« «⁄·«‰ùÂ« ‰„«?‘ œ«œÂ ‘Ê‰œø
        /// </summary>
        public bool ShowNotifications { get; set; }

        /// <summary>
        /// ”ÿÕ —„“‰ê«—?
        /// </summary>
        public EncryptionLevel EncryptionLevel { get; set; }

        /// <summary>
        /// ”«“‰œÂ Å?‘ù›—÷
        /// </summary>
        public ConnectionSettings()
        {
            // „ﬁ«œ?— Å?‘ù›—÷
            SaveCredentials = false;
            AutoConnect = false;
            AutoAcceptConnections = false;
            PlaySoundOnConnection = true;
            ShowNotifications = true;
            EncryptionLevel = EncryptionLevel.High;
        }
    }

    /// <summary>
    /// ”ÿÕ —„“‰ê«—?
    /// </summary>
    public enum EncryptionLevel
    {
        /// <summary>
        /// »œÊ‰ —„“‰ê«—?
        /// </summary>
        None = 0,

        /// <summary>
        /// —„“‰ê«—? Å«?Â
        /// </summary>
        Basic = 1,

        /// <summary>
        /// —„“‰ê«—? „ Ê”ÿ
        /// </summary>
        Medium = 2,

        /// <summary>
        /// —„“‰ê«—? »«·«
        /// </summary>
        High = 3
    }
}