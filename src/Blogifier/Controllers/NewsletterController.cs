using Blogifier.Core.Providers;
using Blogifier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class NewsletterController : ControllerBase
	{
		protected readonly INewsletterProvider _newsletterProvider;

		public NewsletterController(INewsletterProvider newsletterProvider)
		{
			_newsletterProvider = newsletterProvider;
		}

		[HttpPost("subscribe")]
		public async Task<ActionResult<bool>> Subscribe([FromBody] Subscriber subscriber)
		{
			return await _newsletterProvider.AddSubscriber(subscriber);
		}

		[Authorize]
		[HttpGet("subscribers")]
		public async Task<List<Subscriber>> GetSubscribers()
		{
			return await _newsletterProvider.GetSubscribers();
		}

		[HttpDelete("unsubscribe/{id:Guid}")]
		public async Task<ActionResult<bool>> RemoveSubscriber(Guid id)
		{
			return await _newsletterProvider.RemoveSubscriber(id);
		}

		[Authorize]
		[HttpGet("newsletters")]
		public async Task<List<Newsletter>> GetNewsletters()
		{
			return await _newsletterProvider.GetNewsletters();
		}

		[Authorize]
		[HttpGet("send/{postId:Guid}")]
		public async Task<bool> SendNewsletter(Guid postId)
		{
			return await _newsletterProvider.SendNewsletter(postId);
		}

		[Authorize]
		[HttpDelete("remove/{id:Guid}")]
		public async Task<ActionResult<bool>> RemoveNewsletter(Guid id)
		{
			return await _newsletterProvider.RemoveNewsletter(id);
		}

		[Authorize]
		[HttpGet("mailsettings")]
		public async Task<MailSetting> GetMailSettings()
		{
			return await _newsletterProvider.GetMailSettings();
		}

		[Authorize]
		[HttpPut("mailsettings")]
		public async Task<ActionResult<bool>> SaveMailSettings([FromBody] MailSetting mailSettings)
		{
			return await _newsletterProvider.SaveMailSettings(mailSettings);
		}
	}
}
