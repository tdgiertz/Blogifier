using Blogifier.Core.Extensions;
using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Core.Providers.MongoDb.Models;
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
        private readonly IMongoCollection<Author> _authorCollection;
        private readonly ICategoryProvider _categoryProvider;
        private readonly IBlogProvider _blogProvider;

        public PostProvider(IMongoDatabase db, ICategoryProvider categoryProvider, IBlogProvider blogProvider, IAuthorProvider authorProvider)
        {
            _postCollection = db.GetNamedCollection<Post>();
            _authorCollection = db.GetNamedCollection<Author>();
            _categoryProvider = categoryProvider;
            _blogProvider = blogProvider;
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
            {
                return await _postCollection.Find(_ => true).ToListAsync();
            }
            else
            {
                return await _postCollection
                    .Find(p => p.Title.ToLower().Contains(term.ToLower()))
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<PostItem>> Search(PagingDescriptor pagingDescriptor, string term, Guid author = default(Guid), string include = "", bool sanitize = false)
        {
            return await Search(pagingDescriptor, term, author, include, sanitize, null);
        }

        private async Task<IEnumerable<PostItem>> Search(PagingDescriptor pagingDescriptor, string term, Guid author = default(Guid), string include = "", bool sanitize = false, FilterDefinition<Post> predicate = null)
        {
            term = term.ToLower();

            var results = new List<SearchResult>();
            var termList = term.ToLower().Split(' ').ToList();

            foreach (var p in await GetAllPosts(include, author, termList, string.Empty, predicate))
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
                    results.Add(new SearchResult { Rank = rank, Item = PostToItem(p, sanitize) });
                }
            }

            pagingDescriptor.SetTotalCount(results.Count);

            return results.OrderByDescending(r => r.Rank).Skip(pagingDescriptor.Skip).Take(pagingDescriptor.PageSize).Select(r => r.Item).ToList();
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

            var post = await _postCollection.Aggregate()
                .Match(p => p.Slug == slug)
                .Lookup(_authorCollection, p => p.AuthorId, a => a.Id, (Models.PostAuthorAggregate paa) => paa.Author)
                .Unwind<PostAuthorAggregate, LookupPost>(p => p.Author)
                .FirstOrDefaultAsync();

            if(post == null)
            {
                return model;
            }

            model.Post = PostToItem(post.ToPost());

            var relatedTask = Search(new PagingDescriptor(1), model.Post.Title, default(Guid), "PF", true, Builders<Post>.Filter.Ne(p => p.Id, model.Post.Id));
            var previousPostTask = _postCollection.Find(p => p.Published > DateTime.MinValue && p.Published < post.Published).SortByDescending(p => p.Published).FirstOrDefaultAsync();
            var nextPostTask = _postCollection.Find(p => p.Published > DateTime.MinValue && p.Published > post.Published).SortBy(p => p.Published).FirstOrDefaultAsync();
            var updateTask = _postCollection.UpdateOneAsync(p => p.Slug == slug, Builders<Post>.Update.Inc(p => p.PostViews, 1));

            model.Older = PostToItem(await previousPostTask);
            model.Newer = PostToItem(await nextPostTask);

            await updateTask;

            model.Related = (await relatedTask).Take(3).ToList();

            return model;
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

            await _postCollection.InsertOneAsync((Post)post);

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

        public async Task<IEnumerable<PostItem>> GetList(PagingDescriptor pagingDescriptor, Guid author = default(Guid), string category = "", string include = "", bool sanitize = true)
        {
            var posts = await GetPagedPostsAsync(pagingDescriptor, include, author, category);

            return PostToPostItems(posts);
        }

        private async Task<List<Post>> GetPostsCategoryContentAsync(string content)
        {
            var filter = Builders<Post>.Filter.Regex($"{nameof(Category.Content)}", new BsonRegularExpression(content, "i"));
            return await _postCollection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<PostItem>> GetPopular(PagingDescriptor pagingDescriptor, Guid author = default(Guid))
        {
            var builder = Builders<Post>.Filter;
            var filter = author != default(Guid)
                    ? builder.And(builder.Gt(p => p.Published, DateTime.MinValue), builder.Eq(p => p.AuthorId, author))
                    : builder.And(builder.Gt(p => p.Published, DateTime.MinValue));

            var posts = await _postCollection.GetPagedAsync(pagingDescriptor, filter, new[] { new Models.SortDefinition<LookupPost> { IsDescending = true, Sort = p => p.Published } }, aggregateFluent =>
            {
                var aggregate = aggregateFluent
                    .Lookup(_authorCollection, p => p.AuthorId, a => a.Id, (Models.PostAuthorAggregate paa) => paa.Author)
                    .Unwind<PostAuthorAggregate, LookupPost>(p => p.Author);

                return aggregate;
            }, lookupPosts => lookupPosts?.Select(lp => lp.ToPost())?.ToList());

            return PostToPostItems(posts);
        }

        private IEnumerable<PostItem> PostToPostItems(IEnumerable<Post> posts)
        {
            return posts.Select(p => PostToItem(p));
        }

        public async Task<bool> Remove(Guid id)
        {
            var result = await _postCollection.DeleteOneAsync(p => p.Id == id);

            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        #region Private methods

        private PostItem PostToItem(Post post, bool sanitize = false)
        {
            if(post == null)
            {
                return null;
            }

            var postItem = new PostItem
            {
                Id = post.Id,
                PostType = post.PostType,
                Slug = post.Slug,
                Title = post.Title,
                Description = post.Description,
                Content = post.Content,
                Categories = post.Categories,
                Cover = post.Cover,
                PostViews = post.PostViews,
                Rating = post.Rating,
                Published = post.Published,
                Featured = post.IsFeatured,
                Author = post.Author,
                SocialFields = new List<SocialField>()
            };

            if (postItem.Author != null)
            {
                if (string.IsNullOrEmpty(postItem.Author.Avatar))
                    string.Format(Constants.AvatarDataImage, postItem.Author.DisplayName.Substring(0, 1).ToUpper());

                postItem.Author.Email = sanitize ? "donotreply@us.com" : postItem.Author.Email;
            }

            return postItem;
        }

        private async Task<List<Post>> GetAllPosts(string include, Guid author, IEnumerable<string> searchTerms = null, string category = "", FilterDefinition<Post> predicate = null)
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

            if(predicate != null)
            {
                filter = Builders<Post>.Filter.And(filter, predicate);
            }

            var lookupPosts = await _postCollection
                .Aggregate()
                .Match(filter)
                .SortByDescending(p => p.Published)
                .Lookup(_authorCollection, p => p.AuthorId, a => a.Id, (Models.PostAuthorAggregate paa) => paa.Author)
                .Unwind<PostAuthorAggregate, LookupPost>(p => p.Author)
                .ToListAsync();

            return lookupPosts.Select(lp => lp.ToPost()).ToList();
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

        private async Task<List<Post>> GetPagedPostsAsync(PagingDescriptor pagingDescriptor, string include, Guid author, string category = "")
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

            return await _postCollection.GetPagedAsync(pagingDescriptor, filter, new[] { new Models.SortDefinition<LookupPost> { IsDescending = true, Sort = p => p.Published } }, aggregateFluent =>
            {
                var aggregate = aggregateFluent
                    .Lookup(_authorCollection, p => p.AuthorId, a => a.Id, (Models.PostAuthorAggregate paa) => paa.Author)
                    .Unwind<PostAuthorAggregate, LookupPost>(p => p.Author);

                return aggregate;
            }, lookupPosts => lookupPosts?.Select(lp => lp.ToPost())?.ToList());
        }

        private static IAggregateFluent<Post> GetProjection(IAggregateFluent<Models.PostAuthorAggregate> aggregate)
        {
            return aggregate
                .Project(postAuthor => new Post
                {
                    Id = postAuthor.Id,
                    AuthorId = postAuthor.AuthorId,
                    BlogId = postAuthor.BlogId,
                    PostType = postAuthor.PostType,
                    Title = postAuthor.Title,
                    Slug = postAuthor.Slug,
                    Description = postAuthor.Description,
                    Content = postAuthor.Content,
                    Cover = postAuthor.Cover,
                    PostViews = postAuthor.PostViews,
                    Rating = postAuthor.Rating,
                    IsFeatured = postAuthor.IsFeatured,
                    Selected = postAuthor.Selected,
                    Published = postAuthor.Published,
                    DateCreated = postAuthor.DateCreated,
                    DateUpdated = postAuthor.DateUpdated,
                    Categories = postAuthor.Categories,
                    Author = new Author
                    {
                        Id = postAuthor.Author.First().Id,
                        Email = postAuthor.Author.First().Email,
                        DisplayName = postAuthor.Author.First().DisplayName,
                        Bio = postAuthor.Author.First().Bio,
                        BlogId = postAuthor.Author.First().BlogId,
                        Avatar = postAuthor.Author.First().Avatar,
                        IsAdmin = postAuthor.Author.First().IsAdmin,
                        DateCreated = postAuthor.Author.First().DateCreated,
                        DateUpdated = postAuthor.Author.First().DateUpdated
                    }
                });
        }

        #endregion
    }
}
