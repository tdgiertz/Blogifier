using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface INewsletterProvider
	{
		Task<List<Subscriber>> GetSubscribers();
		Task<bool> AddSubscriber(Subscriber subscriber);
		Task<bool> RemoveSubscriber(Guid id);

		Task<List<Newsletter>> GetNewsletters();
		Task<bool> SendNewsletter(Guid postId);
		Task<bool> RemoveNewsletter(Guid id);

		Task<MailSetting> GetMailSettings();
		Task<bool> SaveMailSettings(MailSetting mail);
	}
}
