using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;

namespace Blogifier.Providers
{
    public class GoogleSecretConfigurationSource : IConfigurationSource
    {
        public SecretManagerServiceClient Client { get; set; }
        public string ProjectId { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new GoogleSecretConfigurationProvider(Client, ProjectId);
        }
    }
}
