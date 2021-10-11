using Blogifier.Core.Data;
using Blogifier.Files.Providers;
using Blogifier.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.EfCore
{
    public class FileDescriptorProvider : IFileDescriptorProvider
    {
        private readonly AppDbContext _db;

        public FileDescriptorProvider(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var fileDescriptor = await GetAsync(id);
            _db.FileDescriptors.Remove(fileDescriptor);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<FileDescriptor> GetAsync(Guid id)
        {
            return await _db.FileDescriptors.FindAsync(id);
        }

        public async Task<IEnumerable<FileDescriptor>> GetPagedAsync(PagingDescriptor pagingDescriptor, string searchTerm)
        {
            var query = _db.FileDescriptors.Where(d =>
                d.Filename.ToLower().Contains(searchTerm.ToLower()) ||
                d.Description.ToLower().Contains(searchTerm.ToLower()));

            return await PaginatedList<FileDescriptor>.CreateAsync(query, pagingDescriptor);
        }

        public async Task<bool> InsertAsync(FileDescriptor fileDescriptor)
        {
            _db.FileDescriptors.Add(fileDescriptor);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(FileDescriptor fileDescriptor)
        {
            _db.FileDescriptors.Update(fileDescriptor);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<FileDescriptor>> ListAsync()
        {
           return await _db.FileDescriptors.ToListAsync();
        }
    }
}
