using Blogifier.Core.Extensions;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Shared;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
    public class NewsletterProvider : INewsletterProvider
    {
        private readonly IMongoCollection<Newsletter> _newsletterCollection;
        private readonly IMongoCollection<Subscriber> _subscriberCollection;
        private readonly IMongoCollection<MailSetting> _mailSettingCollection;
        private readonly IEmailProvider _emailProvider;
        private readonly IPostProvider _postProvider;
        private readonly IBlogProvider _blogProvider;

        public NewsletterProvider(IMongoDatabase db, IEmailProvider emailProvider, IPostProvider postProvider, IBlogProvider blogProvider)
        {
            _newsletterCollection = db.GetNamedCollection<Newsletter>();
            _subscriberCollection = db.GetNamedCollection<Subscriber>();
            _mailSettingCollection = db.GetNamedCollection<MailSetting>();
            _emailProvider = emailProvider;
            _postProvider = postProvider;
            _blogProvider = blogProvider;
        }

        public async Task<List<Subscriber>> GetSubscribers()
        {
            return await _subscriberCollection.Find(_ => true).SortByDescending(s => s.Id).ToListAsync();
        }

        public async Task<bool> AddSubscriber(Subscriber subscriber)
        {
            var existing = await _subscriberCollection.Find(s => s.Email == subscriber.Email).FirstOrDefaultAsync();
            if (existing == null)
            {
                subscriber.DateCreated = DateTime.UtcNow;
                await _subscriberCollection.InsertOneAsync(subscriber);
            }
            return true;
        }

        public async Task<bool> RemoveSubscriber(Guid id)
        {
            var result = await _subscriberCollection.DeleteOneAsync(s => s.Id == id);

            return result.IsAcknowledged && result.DeletedCount > 0;
        }


        public async Task<List<Newsletter>> GetNewsletters()
        {
            return await _newsletterCollection
                .Find(_ => true)
                .SortByDescending(n => n.Id)
                .ToListAsync();
        }

        private async Task<bool> SaveNewsletter(Guid postId, bool success)
        {
            var existingCount = await _newsletterCollection.Find(n => n.PostId == postId).CountDocumentsAsync();

            var isSuccessful = true;

            if (existingCount != 0)
            {
                var updateDefinition = Builders<Newsletter>.Update
                    .Set(n => n.DateUpdated, DateTime.UtcNow)
                    .Set(n => n.Success, success);

                var result = await _newsletterCollection.UpdateOneAsync(n => n.PostId == postId, updateDefinition);

                isSuccessful = result.IsAcknowledged;
            }
            else
            {
                var post = await _postProvider.GetPostById(postId);
                var newsletter = new Newsletter()
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    DateCreated = DateTime.UtcNow,
                    Success = success,
                    Post = post
                };
                await _newsletterCollection.InsertOneAsync(newsletter);
            }

            return isSuccessful;
        }

        public async Task<bool> SendNewsletter(Guid postId)
        {
            var post = await _postProvider.GetPostById(postId);
            if (post == null)
            {
                return false;
            }

            var subscribers = await _subscriberCollection.Find(_ => true).ToListAsync();
            if (subscribers == null || subscribers.Count == 0)
            {
                return false;
            }

            var settings = await _mailSettingCollection.Find(_ => true).FirstOrDefaultAsync();
            if (settings == null || settings.Enabled == false)
            {
                return false;
            }

            string subject = post.Title;
            string content = post.Content.MdToHtml();

            bool sent = await _emailProvider.SendEmail(settings, subscribers, subject, content);
            bool saved = await SaveNewsletter(postId, sent);

            return sent && saved;
        }

        public async Task<bool> RemoveNewsletter(Guid id)
        {
            var result = await _newsletterCollection.DeleteOneAsync(s => s.Id == id);

            return result.IsAcknowledged && result.DeletedCount > 0;
        }


        public async Task<MailSetting> GetMailSettings()
        {
            var settings = await _mailSettingCollection.Find(_ => true).FirstOrDefaultAsync();
            return settings == null ? new MailSetting { Id = Guid.NewGuid() } : settings;
        }

        public async Task<bool> SaveMailSettings(MailSetting mail)
        {
            var existing = await _mailSettingCollection.Find(_ => true).FirstOrDefaultAsync();

            var isSuccessful = true;

            if (existing == null)
            {
                var blog = await _blogProvider.GetBlog();
                var newMail = new MailSetting()
                {
                    Id = Guid.NewGuid(),
                    Host = mail.Host,
                    Port = mail.Port,
                    UserEmail = mail.UserEmail,
                    UserPassword = mail.UserPassword,
                    FromEmail = mail.FromEmail,
                    FromName = mail.FromName,
                    ToName = mail.ToName,
                    Enabled = mail.Enabled,
                    DateCreated = DateTime.UtcNow,
                    BlogId = blog.Id
                };

                await _mailSettingCollection.InsertOneAsync(newMail);
            }
            else
            {
                existing.Host = mail.Host;
                existing.Port = mail.Port;
                existing.UserEmail = mail.UserEmail;
                existing.UserPassword = mail.UserPassword;
                existing.FromEmail = mail.FromEmail;
                existing.FromName = mail.FromName;
                existing.ToName = mail.ToName;
                existing.Enabled = mail.Enabled;

                var result = await _mailSettingCollection.ReplaceOneAsync(m => m.Id == existing.Id, existing);

                isSuccessful = result.IsAcknowledged && result.ModifiedCount > 0;
            }

            return isSuccessful;
        }
    }
}
