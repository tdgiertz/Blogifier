using Blogifier.Core.Extensions;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Core.Providers.MongoDb.Models;
using Blogifier.Shared;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
	public class AuthorProvider : IAuthorProvider
	{
        private readonly IMongoCollection<Author> _authorCollection;
        private readonly IBlogProvider _blogProvider;
		private readonly string _salt;

		public AuthorProvider(IMongoDatabase db, IConfiguration configuration, IBlogProvider blogProvider)
		{
            _authorCollection = db.GetNamedCollection<Author>();
            _blogProvider = blogProvider;
			_salt = configuration.GetSection("Blogifier").GetValue<string>("Salt");
		}

		public async Task<List<Author>> GetAuthors()
		{
			return await _authorCollection.Find(_ => true).ToListAsync();
		}

		public async Task<Author> GetAuthorAsync(Guid id)
		{
			return await _authorCollection.Find(a => a.Id == id).FirstOrDefaultAsync();
		}

		public async Task<Author> FindByEmail(string email)
		{
			return await _authorCollection.Find(a => a.Email == email).FirstOrDefaultAsync();
		}

		public async Task<bool> Verify(LoginModel model)
		{
			Serilog.Log.Warning($"Verifying password for {model.Email}");

			var existing = await _authorCollection.Find(a => a.Email == model.Email).FirstOrDefaultAsync();

			if (existing == null)
			{
				Serilog.Log.Warning($"User with email {model.Email} not found");
				return false;
			}

			if(existing.Password == model.Password.Hash(_salt))
			{
				Serilog.Log.Warning($"Successful login for {model.Email}");
				return true;
			}
			else
			{
				Serilog.Log.Warning($"Password does not match");
				return false;
			}
		}

		public async Task<bool> Register(RegisterModel model)
		{
			bool isAdmin = false;
			var author = await _authorCollection.Find(a => a.Email == model.Email).FirstOrDefaultAsync();
			if (author != null)
            {
				return false;
            }

			var blog = (MongoBlog)await _blogProvider.TryGetBlog();
			if (blog == null)
			{
				isAdmin = true; // first blog record - set user as admin
				blog = new MongoBlog
				{
                    Id = Guid.NewGuid(),
					Title = "Blog Title",
					Description = "Short Blog Description",
					Theme = "Standard",
					ItemsPerPage = 10,
					DateCreated = DateTime.UtcNow
				};

				try
				{
					await _blogProvider.AddAsync(blog);
				}
				catch (Exception ex)
				{
					Serilog.Log.Warning($"Error registering new blog: {ex.Message}");
					return false;
				}
			}

			author = new Author
			{
                Id = Guid.NewGuid(),
				DisplayName = model.Name,
				Email = model.Email,
				Password = model.Password.Hash(_salt),
				IsAdmin = isAdmin,
				Avatar = string.Format(Constants.AvatarDataImage, model.Name.Substring(0, 1).ToUpper()),
				Bio = "The short author bio.",
				DateCreated = DateTime.UtcNow,
                BlogId = blog.Id
			};

            await _authorCollection.InsertOneAsync(author);

			blog.AuthorIds ??= new List<Guid>();
			blog.AuthorIds.Add(author.Id);

			return await _blogProvider.Update(blog);
		}

		public async Task<bool> Add(Author author)
		{
			var existing = await _authorCollection.Find(a => a.Email == author.Email).SortBy(a => a.Id).FirstOrDefaultAsync();
			if (existing != null)
            {
				return false;
            }

			var blog = (MongoBlog)await _blogProvider.TryGetBlog();
			if (blog == null)
            {
				return false;
            }

            author.IsAdmin = false;
            author.Password = author.Password.Hash(_salt);
            author.Avatar = string.Format(Constants.AvatarDataImage, author.DisplayName.Substring(0, 1).ToUpper());
            author.DateCreated = DateTime.UtcNow;

            await _authorCollection.InsertOneAsync(author);

			blog.AuthorIds ??= new List<Guid>();
			blog.AuthorIds.Add(author.Id);

			return await _blogProvider.Update(blog);
		}

		public async Task<bool> Update(Author author)
		{
			var existingCount = await _authorCollection
				.Find(a => a.Email == author.Email)
				.CountDocumentsAsync();

			if (existingCount == 0)
            {
				return false;
            }

            if(!author.IsAdmin)
            {
                var adminCount = await _authorCollection.Find(a => a.Email == author.Email && a.IsAdmin).CountDocumentsAsync();
                if (adminCount > 0)
                {
                    return false;
                }
            }

            var updateDefinition = Builders<Author>.Update
                .Set(a => a.DisplayName, author.DisplayName)
                .Set(a => a.Bio, author.Bio)
                .Set(a => a.Avatar, author.Avatar)
                .Set(a => a.IsAdmin, author.IsAdmin);

            var result = await _authorCollection.UpdateOneAsync(a => a.Email == author.Email, updateDefinition);

			return result.IsAcknowledged && result.ModifiedCount > 0;
		}

		public async Task<bool> ChangePassword(RegisterModel model)
		{
			var existingCount = await _authorCollection
				.Find(a => a.Email == model.Email)
				.CountDocumentsAsync();

			if (existingCount == 0)
            {
				return false;
            }

            var updateDefinition = Builders<Author>.Update
                .Set(a => a.Password, model.Password.Hash(_salt));

            var result = await _authorCollection.UpdateOneAsync(a => a.Email == model.Email, updateDefinition);

			return result.IsAcknowledged && result.ModifiedCount > 0;
		}

		public async Task<bool> Remove(Guid id)
		{
            var result = await _authorCollection.DeleteOneAsync(a => a.Id == id);

            return result.IsAcknowledged && result.DeletedCount > 0;
		}
	}
}
