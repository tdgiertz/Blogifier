using Blogifier.Shared;
using MongoDB.Driver;
using System.Reflection;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
    public class AboutProvider : IAboutProvider
    {
        private readonly IMongoDatabase _db;

        public AboutProvider(IMongoDatabase db)
        {
            _db = db;
        }

        public async Task<AboutModel> GetAboutModel()
        {
            var model = new AboutModel();

            model.Version = typeof(AboutProvider)
                   .GetTypeInfo()
                   .Assembly
                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   .InformationalVersion;

            model.DatabaseProvider = _db.GetType().Namespace;

            model.OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

            return await Task.FromResult(model);
        }
    }
}
