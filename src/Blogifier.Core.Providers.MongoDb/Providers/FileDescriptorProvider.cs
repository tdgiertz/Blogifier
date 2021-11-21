using Blogifier.Core.Providers.MongoDb.Extensions;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
    public class FileDescriptorProvider : IFileDescriptorProvider
    {
        private readonly IMongoCollection<FileDescriptor> _fileCollection;

        public FileDescriptorProvider(IMongoDatabase db)
        {
            _fileCollection = db.GetNamedCollection<FileDescriptor>();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _fileCollection.DeleteOneAsync(f => f.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        public async Task<FileDescriptor> GetAsync(Guid id)
        {
            return await _fileCollection.Find(f => f.Id == id).SingleOrDefaultAsync();
        }

        public async Task<FileDescriptor> GetAsync(string filePath)
        {
            return await _fileCollection.Find(f => f.RelativePath == filePath).SingleOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(string filePath)
        {
            var count = await _fileCollection.Find(f => f.RelativePath == filePath).CountDocumentsAsync();
            return count > 0;
        }

        public async Task<IEnumerable<FileDescriptor>> GetPagedAsync(InfinitePagingDescriptor pagingDescriptor, string searchTerm)
        {
            var builder = Builders<FileDescriptor>.Filter;

            var pageFilter = builder.Empty;

            if(pagingDescriptor.LastDateTime != null)
            {
                pageFilter = builder.Lt(d => d.DateCreated, pagingDescriptor.LastDateTime);
            }

            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter = builder.Or(
                    builder.Regex(d => d.Filename, new BsonRegularExpression(searchTerm, "i")),
                    builder.Regex(d => d.Description, new BsonRegularExpression(searchTerm, "i")));
            }

            filter = builder.And(pageFilter, filter);

            var sortDefinitions = new[]
            {
                new Models.SortDefinition<FileDescriptor> { IsDescending = true, Sort = d => d.DateCreated },
                new Models.SortDefinition<FileDescriptor> { IsDescending = true, Sort = d => d.Id }
            };

            var result = await _fileCollection
                .Find(filter)
                .SortByDescending(d => d.DateCreated)
                .ThenByDescending(d => d.Id)
                .Limit(pagingDescriptor.PageSize)
                .ToListAsync();

            return result;

            // return await _fileCollection.GetPagedAsync(pagingDescriptor, filter, sortDefinitions);
        }

        public async Task<IEnumerable<FileDescriptor>> GetPagedAsync(PagingDescriptor pagingDescriptor, string searchTerm)
        {
            var builder = Builders<FileDescriptor>.Filter;

            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter = builder.Or(
                    builder.Regex(d => d.Filename, new BsonRegularExpression(searchTerm, "i")),
                    builder.Regex(d => d.Description, new BsonRegularExpression(searchTerm, "i")));
            }

            var sortDefinitions = new[] { new Models.SortDefinition<FileDescriptor> { IsDescending = true, Sort = d => d.DateCreated } };

            return await _fileCollection.GetPagedAsync(pagingDescriptor, filter, sortDefinitions);
        }

        public async Task<bool> InsertAsync(FileDescriptor fileDescriptor)
        {
            try
            {
                await _fileCollection.InsertOneAsync(fileDescriptor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to insert to MongoDb");
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateAsync(FileDescriptor fileDescriptor)
        {
            var update = Builders<FileDescriptor>.Update
                .Set(d => d.MimeType, fileDescriptor.MimeType)
                .Set(d => d.Description, fileDescriptor.Description)
                .Set(d => d.Filename, fileDescriptor.Filename)
                .Set(d => d.RelativePath, fileDescriptor.RelativePath)
                .Set(d => d.ThumbnailRelativePath, fileDescriptor.ThumbnailRelativePath)
                .Set(d => d.DateUpdated, DateTime.UtcNow);

            var result = await _fileCollection.UpdateOneAsync(d => d.Id == fileDescriptor.Id, update);

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<IEnumerable<FileDescriptor>> ListAsync()
        {
            return await _fileCollection.Find(_ => true).ToListAsync();
        }
    }
}
