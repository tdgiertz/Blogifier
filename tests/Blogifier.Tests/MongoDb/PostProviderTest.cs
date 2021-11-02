using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blogifier.Core.Providers;
using Blogifier.Core.Providers.MongoDb;
using Blogifier.Shared;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Blogifier.Tests.MongoDb
{
    public class PostProviderTest : MongoIntegrationTest
    {
        [Fact]
        public async Task Has_Correct_Property_Setup()
        {
            var post1 = await _postCollection.Find(p => p.Id == _post1Id).FirstOrDefaultAsync();

            Assert.Equal(_authorId, post1.AuthorId);
            Assert.Null(post1.Author);
        }

        [Fact]
        public async Task Can_Join_Posts_With_Authors()
        {
            var provider = GetPostProvider();

            var dbPosts = await _postCollection.Find(_ => true).ToListAsync();

            foreach(var post in dbPosts)
            {
                Assert.Null(post.Author);
            }

            var pager = new Pager(0, 10);

            var posts = await provider.GetList(pager);

            Assert.Equal(4, posts.Count());
            foreach(var post in posts)
            {
                Assert.NotNull(post.Author);
            }
        }

        [Fact]
        public async Task Can_Get_Categories()
        {
            var provider = GetPostProvider();

            var pager = new Pager(1, 2);

            var posts = await provider.Search(pager, "Test");

            var post1 = posts.FirstOrDefault(p => p.Title == "Post 1");
            var post3 = posts.FirstOrDefault(p => p.Title == "Post 3 Test");

            Assert.Equal(2, posts.Count());
            Assert.NotNull(post1);
            Assert.NotNull(post3);
        }

        private PostProvider GetPostProvider()
        {
            var categoryProvider = new CategoryProvider(_database);
            var storageProvider = new Mock<IStorageProvider>();
            storageProvider.Setup(s => s.GetThemeSettings(It.IsAny<string>())).Returns(Task.FromResult(new Shared.ThemeSettings { }));
            var blogProvider = new BlogProvider(_database, storageProvider.Object, categoryProvider);
            var authorProvider = new AuthorProvider(_database, _configuration, blogProvider);
            return new PostProvider(_database, categoryProvider, blogProvider, authorProvider);
        }
    }
}
