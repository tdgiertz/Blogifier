using Blogifier.Core.Extensions;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Shared;
using Blogifier.Shared.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
    public class PostProvider : IPostProvider
    {
        private readonly IMongoCollection<Post> _postCollection;
        private readonly ICategoryProvider _categoryProvider;
        private readonly IBlogProvider _blogProvider;
        private readonly IAuthorProvider _authorProvider;

        public PostProvider(IMongoDatabase db, ICategoryProvider categoryProvider, IBlogProvider blogProvider, IAuthorProvider authorProvider)
        {
            _postCollection = db.GetNamedCollection<Post>();
            _categoryProvider = categoryProvider;
            _blogProvider = blogProvider;
            _authorProvider = authorProvider;
        }

        public async Task<List<Post>> GetPosts(PublishedStatus filter, PostType postType)
        {
            var predicate = GetExpression(p => p.PostType == postType);
            switch (filter)
            {
                case PublishedStatus.Published:
                    predicate = GetExpression(p => p.PostType == postType && p.Published > DateTime.MinValue);
                    break;
                case PublishedStatus.Drafts:
                    predicate = GetExpression(p => p.PostType == postType && p.Published == DateTime.MinValue);
                    break;
                case PublishedStatus.Featured:
                    predicate = GetExpression(p => p.PostType == postType && p.IsFeatured);
                    break;
            }
            return await _postCollection.Find(predicate).SortByDescending(p => p.Id).ToListAsync();
        }

        private Expression<Func<Post, bool>> GetExpression(Expression<Func<Post, bool>> exp)
        {
            return exp;
        }

        public async Task<List<Post>> SearchPosts(string term)
        {
            if (term == "*")
                return await _postCollection.Find(_ => true).ToListAsync();

            return await _postCollection
                .Find(p => p.Title.ToLower().Contains(term.ToLower()))
                .ToListAsync();
        }

        public async Task<IEnumerable<PostItem>> Search(Pager pager, string term, Guid author = default(Guid), string include = "", bool sanitize = false)
        {
            var skip = (pager.CurrentPage - 1) * pager.ItemsPerPage;
            term = term.ToLower();

            var results = new List<SearchResult>();
            var termList = term.ToLower().Split(' ').ToList();

            foreach (var p in await GetAllPosts(include, author, termList))
            {
                var rank = 0;
                var hits = 0;

                foreach (var termItem in termList)
                {
                    if (termItem.Length < 4 && rank > 0) continue;

                    if (p.Categories != null && p.Categories.Any())
                    {
                        foreach (var category in p.Categories)
                        {
                            if (category.Content.ToLower().Contains(termItem)) rank += 10;
                        }
                    }
                    if (p.Title.ToLower().Contains(termItem))
                    {
                        hits = Regex.Matches(p.Title.ToLower(), termItem).Count;
                        rank += hits * 10;
                    }
                    if (p.Description.ToLower().Contains(termItem))
                    {
                        hits = Regex.Matches(p.Description.ToLower(), termItem).Count;
                        rank += hits * 3;
                    }
                    if (p.Content.ToLower().Contains(termItem))
                    {
                        rank += Regex.Matches(p.Content.ToLower(), termItem).Count;
                    }
                }
                if (rank > 0)
                {
                    results.Add(new SearchResult { Rank = rank, Item = await PostToItem(p, sanitize) });
                }
            }

            pager.Configure(results.Count);

            return results.OrderByDescending(r => r.Rank).Skip(skip).Take(pager.ItemsPerPage).Select(r => r.Item).ToList();
        }

        public async Task<Post> GetPostById(Guid id)
        {
            return await _postCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PostItem>> GetPostItems()
        {
            var posts = await _postCollection.Find(_ => true).ToListAsync();
            var postItems = new List<PostItem>();

            foreach (var post in posts)
            {
                postItems.Add(new PostItem
                {
                    Id = post.Id,
                    Title = post.Title,
                    Description = post.Description,
                    Content = post.Content,
                    Slug = post.Slug,
                    Author = post.Author,
                    Cover = string.IsNullOrEmpty(post.Cover) ? Constants.DefaultCover : post.Cover,
                    Published = post.Published,
                    PostViews = post.PostViews,
                    Featured = post.IsFeatured
                });
            }

            return postItems;
        }

        public async Task<PostModel> GetPostModel(string slug)
        {
            var model = new PostModel();

            var all = _postCollection
                .Find(_ => true)
                .SortByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Published).ToList();

            await SetOlderNewerPosts(slug, model, all);

            var updateDefinition = Builders<Post>.Update
                .Inc(p => p.PostViews, 1);

            var result = _postCollection.UpdateOneAsync(p => p.Slug == slug, updateDefinition);

            model.Related = await Search(new Pager(1), model.Post.Title, default(Guid), "PF", true);
            model.Related = model.Related.Where(r => r.Id != model.Post.Id).ToList();

            return model;
        }

        private async Task SetOlderNewerPosts(string slug, PostModel model, List<Post> all)
        {
            if (all != null && all.Count > 0)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    if (all[i].Slug == slug)
                    {
                        model.Post = await PostToItem(all[i]);

                        if (i > 0 && all[i - 1].Published > DateTime.MinValue)
                            model.Newer = await PostToItem(all[i - 1]);

                        if (i + 1 < all.Count && all[i + 1].Published > DateTime.MinValue)
                            model.Older = await PostToItem(all[i + 1]);

                        break;
                    }
                }
            }
        }

        public async Task<Post> GetPostBySlug(string slug)
        {
            return await _postCollection.Find(p => p.Slug == slug).FirstOrDefaultAsync();
        }

        public async Task<string> GetSlugFromTitle(string title)
        {
            string slug = title.ToSlug();
            var post = await GetPostBySlug(slug);

            if (post != null)
            {
                for (int i = 2; i < 100; i++)
                {
                    slug = $"{slug}{i}";
                    var checkPost = await GetPostBySlug(slug);
                    if (checkPost == null)
                    {
                        return slug;
                    }
                }
            }
            return slug;
        }

        public async Task<bool> Add(Post post)
        {
            var existing = await GetPostBySlug(post.Slug);
            if (existing != null)
                return false;

            post.Blog = await _blogProvider.GetBlog();
            post.BlogId = post.Blog.Id;
            post.DateCreated = DateTime.UtcNow;

            // sanitize HTML fields
            post.Content = post.Content.RemoveScriptTags();
            post.Description = post.Description.RemoveScriptTags();

            await _postCollection.InsertOneAsync(post);

            return true;
        }

        public async Task<bool> Update(Post post)
        {
            var updateDefinition = Builders<Post>.Update
                .Set(p => p.Slug, post.Slug)
                .Set(p => p.Title, post.Title)
                .Set(p => p.Description, post.Description.RemoveScriptTags())
                .Set(p => p.Content, post.Content.RemoveScriptTags())
                .Set(p => p.Cover, post.Cover)
                .Set(p => p.PostType, post.PostType)
                .Set(p => p.Published, post.Published);

            var result = await _postCollection.UpdateOneAsync(p => p.Slug == post.Slug, updateDefinition);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> Publish(Guid id, bool publish)
        {
            var updateDefinition = Builders<Post>.Update
                .Set(p => p.Published, publish ? DateTime.UtcNow : DateTime.MinValue);

            var result = await _postCollection.UpdateOneAsync(p => p.Id == id, updateDefinition);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> Featured(Guid id, bool featured)
        {
            var updateDefinition = Builders<Post>.Update
                .Set(p => p.IsFeatured, featured);

            var result = await _postCollection.UpdateOneAsync(p => p.Id == id, updateDefinition);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<IEnumerable<PostItem>> GetList(Pager pager, Guid author = default(Guid), string category = "", string include = "", bool sanitize = true)
        {
            var posts = await GetPagedPosts(pager, include, author, category);

            return await PostToPostItemsAsync(posts);
        }

        private async Task<List<Post>> GetPostsCategoryContentAsync(string content)
        {
            var filter = Builders<Post>.Filter.Regex($"{nameof(Category.Content)}", new BsonRegularExpression(content, "i"));
            var categories = await _postCollection.Find(filter).ToListAsync();

            return categories;
        }

        public async Task<IEnumerable<PostItem>> GetPopular(Pager pager, Guid author = default(Guid))
        {
            var builder = Builders<Post>.Filter;
            var filter = author != default(Guid)
                    ? builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.AuthorId, author))
                    : builder.And(builder.Gt(p => p.Published, DateTime.MinValue));

            var posts = await GetPagedAsync(_postCollection, pager, filter, new[] { new Models.SortDefinition<Post> { IsDescending = true, Sort = p => p.Published } });

            return await PostToPostItemsAsync(posts);
        }

        private async Task<IEnumerable<PostItem>> PostToPostItemsAsync(IEnumerable<Post> posts)
        {
            var itemTasks = posts.Select(p => PostToItem(p));

            var items = new List<PostItem>();

            foreach(var itemTask in itemTasks)
            {
                items.Add(await itemTask);
            }

            return items;
        }

        public async Task<bool> Remove(Guid id)
        {
            var result = await _postCollection.DeleteOneAsync(p => p.Id == id);

            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        #region Private methods

        private async Task<PostItem> PostToItem(Post p, bool sanitize = false)
        {
            var author = await _authorProvider.GetAuthorAsync(p.AuthorId);

            var post = new PostItem
            {
                Id = p.Id,
                PostType = p.PostType,
                Slug = p.Slug,
                Title = p.Title,
                Description = p.Description,
                Content = p.Content,
                Categories = p.Categories,
                Cover = p.Cover,
                PostViews = p.PostViews,
                Rating = p.Rating,
                Published = p.Published,
                Featured = p.IsFeatured,
                Author = author,
                SocialFields = new List<SocialField>()
            };

            if (post.Author != null)
            {
                if (string.IsNullOrEmpty(post.Author.Avatar))
                    string.Format(Constants.AvatarDataImage, post.Author.DisplayName.Substring(0, 1).ToUpper());

                post.Author.Email = sanitize ? "donotreply@us.com" : post.Author.Email;
            }

            return post;
        }

        private async Task<List<Post>> GetAllPosts(string include, Guid author, IEnumerable<string> searchTerms = null, string category = "")
        {
            FilterDefinition<Post> postDraftFilter = null;
            FilterDefinition<Post> postFeaturedFilter = null;
            FilterDefinition<Post> postPublishedFilter = null;

            var categoryFilter = string.IsNullOrEmpty(category)
                ? null
                : Builders<Post>.Filter.ElemMatch(p => p.Categories, Builders<Category>.Filter.Regex(c => c.Content, new BsonRegularExpression(category.ToLower(), "i")));

            if (include.ToUpper().Contains(Constants.PostDraft) || string.IsNullOrEmpty(include))
            {
                var builder = Builders<Post>.Filter;
                postDraftFilter = author != default(Guid)
                    ? builder.And(builder.Eq(p => p.Published, DateTime.MinValue), builder.Eq(p => p.AuthorId, author), builder.Eq(p => p.PostType, PostType.Post))
                    : builder.And(builder.Eq(p => p.Published, DateTime.MinValue), builder.Eq(p => p.PostType, PostType.Post));
            }

            if (include.ToUpper().Contains(Constants.PostFeatured) || string.IsNullOrEmpty(include))
            {
                var builder = Builders<Post>.Filter;
                postFeaturedFilter = author != default(Guid)
                    ? builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, true), builder.Eq(p => p.AuthorId, author), builder.Eq(p => p.PostType, PostType.Post))
                    : builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, true), builder.Eq(p => p.PostType, PostType.Post));
            }

            if (include.ToUpper().Contains(Constants.PostPublished) || string.IsNullOrEmpty(include))
            {
                var builder = Builders<Post>.Filter;
                postPublishedFilter = author != default(Guid)
                    ? builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, false), builder.Eq(p => p.AuthorId, author), builder.Eq(p => p.PostType, PostType.Post))
                    : builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, false), builder.Eq(p => p.PostType, PostType.Post));
            }

            var filters = new[] { postDraftFilter, postFeaturedFilter, postPublishedFilter }.Where(f => f != null);

            var filter = filters.Any() ? Builders<Post>.Filter.Or(filters) : Builders<Post>.Filter.Empty;

            if (categoryFilter != null)
            {
                filter = Builders<Post>.Filter.And(filter, categoryFilter);
            }

            var searchTermsFilter = GetSearchTermsFilter(searchTerms);

            if (searchTermsFilter != null)
            {
                filter = Builders<Post>.Filter.And(filter, searchTermsFilter);
            }

            return await _postCollection
                .Aggregate()
                .Match(filter)
                .SortByDescending(p => p.Published)
                .ToListAsync();
        }

        private FilterDefinition<Post> GetSearchTermsFilter(IEnumerable<string> searchTerms)
        {
            if (searchTerms == null || !searchTerms.Any()) return null;

            FilterDefinition<Post> definition = Builders<Post>.Filter.Empty;

            foreach (var searchTerm in searchTerms)
            {
                var filter1 = Builders<Post>.Filter.ElemMatch(p => p.Categories, Builders<Category>.Filter.Regex(c => c.Content, new BsonRegularExpression(searchTerm, "i")));
                var filter2 = Builders<Post>.Filter.Regex($"{nameof(Post.Content)}", new BsonRegularExpression(searchTerm, "i"));
                var filter3 = Builders<Post>.Filter.Regex($"{nameof(Post.Title)}", new BsonRegularExpression(searchTerm, "i"));
                var filter4 = Builders<Post>.Filter.Regex($"{nameof(Post.Description)}", new BsonRegularExpression(searchTerm, "i"));

                definition = definition == null
                    ? Builders<Post>.Filter.Or(filter1, filter2, filter3, filter4)
                    : Builders<Post>.Filter.Or(definition, filter1, filter2, filter3, filter4);
            }

            return definition;
        }

        private async Task<List<Post>> GetPagedPosts(Pager pager, string include, Guid author, string category = "")
        {
            FilterDefinition<Post> postDraftFilter = null;
            FilterDefinition<Post> postFeaturedFilter = null;
            FilterDefinition<Post> postPublishedFilter = null;

            var categoryFilter = string.IsNullOrEmpty(category)
                ? null
                : Builders<Post>.Filter.ElemMatch(p => p.Categories, Builders<Category>.Filter.Regex(c => c.Content, new BsonRegularExpression(category.ToLower(), "i")));

            if (include.ToUpper().Contains(Constants.PostDraft) || string.IsNullOrEmpty(include))
            {
                var builder = Builders<Post>.Filter;
                postDraftFilter = author != default(Guid)
                    ? builder.And(builder.Eq(p => p.Published, DateTime.MinValue), builder.Eq(p => p.AuthorId, author), builder.Eq(p => p.PostType, PostType.Post))
                    : builder.And(builder.Eq(p => p.Published, DateTime.MinValue), builder.Eq(p => p.PostType, PostType.Post));
            }

            if (include.ToUpper().Contains(Constants.PostFeatured) || string.IsNullOrEmpty(include))
            {
                var builder = Builders<Post>.Filter;
                postFeaturedFilter = author != default(Guid)
                    ? builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, true), builder.Eq(p => p.AuthorId, author), builder.Eq(p => p.PostType, PostType.Post))
                    : builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, true), builder.Eq(p => p.PostType, PostType.Post));
            }

            if (include.ToUpper().Contains(Constants.PostPublished) || string.IsNullOrEmpty(include))
            {
                var builder = Builders<Post>.Filter;
                postPublishedFilter = author != default(Guid)
                    ? builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, false), builder.Eq(p => p.AuthorId, author), builder.Eq(p => p.PostType, PostType.Post))
                    : builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.IsFeatured, false), builder.Eq(p => p.PostType, PostType.Post));
            }

            var filters = new[] { postDraftFilter, postFeaturedFilter, postPublishedFilter }.Where(f => f != null);

            var filter = filters.Any() ? Builders<Post>.Filter.Or(filters) : null;

            if (categoryFilter != null)
            {
                if (filter == null)
                {
                    filter = categoryFilter;
                }
                else
                {
                    filter = Builders<Post>.Filter.And(filter, categoryFilter);
                }
            }

            return await GetPagedAsync(_postCollection, pager, filter, new[] { new Models.SortDefinition<Post> { IsDescending = true, Sort = p => p.Published } });
        }

        private async Task<List<T>> GetPagedAsync<T>(IMongoCollection<T> collection, Pager pager, FilterDefinition<T> filter, IEnumerable<Models.SortDefinition<T>> sortDefinitions = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            var skip = (pager.CurrentPage - 1) * pager.ItemsPerPage;

            var dataPipelineStageDefinitions = new List<PipelineStageDefinition<T, T>>();

            var sortStageDefinition = GetSortDefinition(sortDefinitions);

            if (sortStageDefinition != null)
            {
                dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Sort(sortStageDefinition));
            }

            dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Skip<T>(skip));
            dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Limit<T>(pager.ItemsPerPage));

            var countFacet = AggregateFacet.Create("count", PipelineDefinition<T, AggregateCountResult>.Create(new[]
                {
                    PipelineStageDefinitionBuilder.Count<T>()
                }));

            var dataFacet = AggregateFacet.Create("data", PipelineDefinition<T, T>.Create(dataPipelineStageDefinitions));

            var aggregation = await collection.Aggregate()
                .Match(filter)
                .Facet(countFacet, dataFacet)
                .ToListAsync();

            var count = aggregation.First()
                .Facets.First(x => x.Name == "count")
                .Output<AggregateCountResult>()
                ?.FirstOrDefault()
                ?.Count ?? 0;

            var data = aggregation.First()
                .Facets.First(x => x.Name == "data")
                .Output<T>();

            pager.Configure(count);

            return data.ToList();
        }

        private MongoDB.Driver.SortDefinition<T> GetSortDefinition<T>(IEnumerable<Models.SortDefinition<T>> sortDefinitions)
        {
            var builder = Builders<T>.Sort;

            if (sortDefinitions != null && sortDefinitions.Any())
            {
                MongoDB.Driver.SortDefinition<T> definition = null;
                foreach (var sortDefinition in sortDefinitions)
                {
                    if (definition != null)
                    {
                        definition = sortDefinition.IsDescending
                            ? definition.Descending(sortDefinition.Sort)
                            : definition.Ascending(sortDefinition.Sort);
                    }
                    else
                    {
                        definition = sortDefinition.IsDescending
                            ? builder.Descending(sortDefinition.Sort)
                            : builder.Ascending(sortDefinition.Sort);
                    }
                }

                return definition;
            }

            return null;
        }

        #endregion
    }
}
