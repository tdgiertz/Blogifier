using Blogifier.Files.Models;
using Blogifier.Files.Aws;

namespace Blogifier.Files.Backblaze
{
    public class BackblazeFileStoreProvider : AwsFileStoreProvider
    {
        public BackblazeFileStoreProvider(FileStoreConfiguration configuration) : base(configuration, configuration.Endpoint)
        {
        }
    }
}
