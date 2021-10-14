using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Iam.v1;
using Google.Apis.Iam.v1.Data;
using Google.Cloud.Storage.V1;

namespace Blogifier.Files.Google
{
    internal sealed class IamServiceBlobSigner : UrlSigner.IBlobSigner
    {
        private readonly IamService _iamService;
        public string Id { get; }

        internal IamServiceBlobSigner(IamService service, string id)
        {
            _iamService = service;
            Id = id;
        }

        public string CreateSignature(byte[] data) => CreateRequest(data).Execute().Signature;

        public async Task<string> CreateSignatureAsync(byte[] data, CancellationToken cancellationToken)
        {
            var request = CreateRequest(data);
            var response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return response.Signature;
        }

        private ProjectsResource.ServiceAccountsResource.SignBlobRequest CreateRequest(byte[] data)
        {
            var body = new SignBlobRequest { BytesToSign = Convert.ToBase64String(data) };
            var account = $"projects/-/serviceAccounts/{Id}";
            var request = _iamService.Projects.ServiceAccounts.SignBlob(body, account);
            return request;
        }
    }
}
