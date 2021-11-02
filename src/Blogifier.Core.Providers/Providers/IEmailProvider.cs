using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface IEmailProvider
	{
		Task<bool> SendEmail(MailSetting settings, List<Subscriber> subscribers, string subject, string content);
	}
}
