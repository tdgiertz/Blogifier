using Microsoft.Extensions.Configuration;
using Blogifier.Providers;
using Google.Cloud.SecretManager.V1;

namespace Blogifier.Extensions
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddGoogleSecretsManager(this IConfigurationBuilder configurationBuilder, string projectId)
        {
            configurationBuilder.Add(new GoogleSecretConfigurationSource
            {
                Client = SecretManagerServiceClient.Create(),
                ProjectId = projectId
            });

            return configurationBuilder;
        }
    }
}
