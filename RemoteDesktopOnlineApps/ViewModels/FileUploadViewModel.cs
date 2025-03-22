using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopOnlineApps.ViewModels
{
    public class FileUploadViewModel
    {
        [Required]
        public int SessionId { get; set; }

        public string ServerName { get; set; }

        [Required(ErrorMessage = "لطفاً حداقل یک فایل انتخاب کنید")]
        public List<IFormFile> Files { get; set; }

        [Required(ErrorMessage = "مسیر مقصد الزامی است")]
        public string DestinationPath { get; set; } = "C:\\Users\\Public\\Downloads";
    }
}