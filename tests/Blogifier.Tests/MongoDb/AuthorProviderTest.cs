using System.Threading.Tasks;
using Blogifier.Core.Providers;
using Blogifier.Core.Providers.MongoDb;
using Blogifier.Shared;
using Moq;
using Xunit;

namespace Blogifier.Tests.MongoDb
{
    public class AuthorProviderTest : MongoIntegrationTest
    {
        [Fact]
        public async Task Can_Register()
        {
            var categoryProvider = new CategoryProvider(_database);
            var storageProvider = new Mock<IStorageProvider>();
            storageProvider.Setup(s => s.GetThemeSettings(It.IsAny<string>())).Returns(Task.FromResult(new Shared.ThemeSettings { }));
            var blogProvider = new BlogProvider(_database, storageProvider.Object, categoryProvider);
            var provider = new AuthorProvider(_database, _configuration, blogProvider);

            var model = new RegisterModel
            {
                Name = "Test",
                Email = "test@example.com",
                Password = "some-fake-p@ssword",
                PasswordConfirm = "some-fake-p@ssword",
            };

            var hasRegistered = await provider.Register(model);

            Assert.True(hasRegistered);
        }
    }
}
