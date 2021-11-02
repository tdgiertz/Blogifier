using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers
{
    public interface IThemeProvider
	{
		Task<Dictionary<string, string>> GetSettings(string theme);
	}
}
