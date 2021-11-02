using System;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;

namespace Blogifier.Providers
{
    public class GoogleSecretConfigurationProvider : ConfigurationProvider
    {
        private readonly SecretManagerServiceClient _client;
        private readonly string _projectId;

        public GoogleSecretConfigurationProvider(SecretManagerServiceClient client, string projectId)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            _client = client;
            _projectId = projectId;
        }

        public override void Load()
        {
            var secrets = _client.ListSecrets(new ProjectName(_projectId));
            foreach (var secret in secrets)
            {
                try
                {
                    var secretVersionName = new SecretVersionName(secret.SecretName.ProjectId, secret.SecretName.SecretId, "latest");

                    var secretVersion = _client.AccessSecretVersion(secretVersionName);

                    var secretId = ReplaceDelimiter(secret.SecretName.SecretId);

                    Set(secretId, secretVersion.Payload.Data.ToStringUtf8());
                }
                catch
                {
                }
            }
        }

        private static string ReplaceDelimiter(string secretId)
        {
            return secretId.Replace("--", ConfigurationPath.KeyDelimiter);
        }
    }
}
