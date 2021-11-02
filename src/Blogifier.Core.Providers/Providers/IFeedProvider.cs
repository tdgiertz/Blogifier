using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SyndicationFeed.Atom;

namespace Blogifier.Core.Providers
{
    public interface IFeedProvider
	{
      Task<IEnumerable<AtomEntry>> GetEntries(string type, string host);
   }
}
