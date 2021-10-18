using Blogifier.Shared;

namespace Blogifier.Admin.Models
{
    public class UploadFileModel
    {
        public UploadState UploadState {get;set;}
        public FileModel FileModel { get; set; }
        public System.IO.Stream FileStream { get; set; }
    }
}
