namespace Blogifier.Files.Models
{
    public class FileStoreConfiguration
    {
        public string AuthenticationKey { get; set; }
        public KeySource AuthenticationKeySource { get; set; }
        public string StoreName { get; set; }
        public string BasePath {get;set;}
        public string ThumbnailBasePath {get;set;}
    }
}
