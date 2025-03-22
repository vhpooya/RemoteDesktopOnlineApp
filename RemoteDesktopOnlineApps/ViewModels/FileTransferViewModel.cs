using RemoteDesktopOnlineApps.Models;
using System.Collections.Generic;

namespace RemoteDesktopOnlineApps.ViewModels
{
    public class FileTransferViewModel
    {
        public int? CurrentSessionId { get; set; }
        public RemoteSession CurrentSession { get; set; }
        public List<RemoteSession> ActiveSessions { get; set; }
        public List<FileTransfer> FileTransfers { get; set; }
    }
}