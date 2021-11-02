namespace Blogifier.Admin.Models
{
    public class UploadState
    {
        public int ProgressPercent { get; set; }
        public bool IsUploading { get; set; }
        public bool IsEditing { get; set; }
        public bool IsUploadPending { get; set; }
    }
}
