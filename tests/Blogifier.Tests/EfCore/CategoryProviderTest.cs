using Blogifier.Core.Data;
using Blogifier.Core.Providers.EfCore;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Blogifier.Tests.EfCore
{
    public class CategoryProviderTest : SqliteInMemoryProviderTest
    {
        [Fact]
        public async Task Can_Get_Categories()
        {
            using (var context = new AppDbContext(ContextOptions))
            {
                var provider = new CategoryProvider(context);

                var categories = await provider.Categories();

                Assert.Equal(3, categories.Count);
                Assert.Equal("category 1", categories[0].Category);
                Assert.Equal("category 2", categories[1].Category);
                Assert.Equal("category 3", categories[2].Category);
                Assert.Equal(1, categories[0].PostCount);
                Assert.Equal(3, categories[1].PostCount);
                Assert.Equal(2, categories[2].PostCount);
            }
        }

        [Fact]
        public async Task Can_Get_Category_By_Id()
        {
            using (var context = new AppDbContext(ContextOptions))
            {
                var provider = new CategoryProvider(context);

                var category = await provider.GetCategory(_category1Id);

                Assert.Equal("Category 1", category.Content);
            }
        }

        [Fact]
        public async Task Can_Get_Post_Category_By_Id()
        {
            using (var context = new AppDbContext(ContextOptions))
            {
                var provider = new CategoryProvider(context);

                var categories = (await provider.GetPostCategories(_post1Id)).ToList();

                categories = categories.OrderBy(c => c.Content).ToList();

                Assert.Equal(3, categories.Count);
                Assert.Equal("Category 1", categories[0].Content);
                Assert.Equal("Category 2", categories[1].Content);
                Assert.Equal("Category 3", categories[2].Content);
            }
        }
    }
}
