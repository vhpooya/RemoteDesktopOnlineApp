using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopOnlineApps.ViewModels
{
    public class FileDownloadViewModel
    {
        [Required]
        public int SessionId { get; set; }

        public string ServerName { get; set; }

        [Required(ErrorMessage = "مسیر فایل در سیستم راه دور الزامی است")]
        public string RemoteFilePath { get; set; }

        [Required(ErrorMessage = "مسیر مقصد محلی الزامی است")]
        public string LocalDestinationPath { get; set; }
    }
}