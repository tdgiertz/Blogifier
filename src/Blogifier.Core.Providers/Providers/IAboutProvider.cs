using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface IAboutProvider
    {
        Task<AboutModel> GetAboutModel();
    }
}
