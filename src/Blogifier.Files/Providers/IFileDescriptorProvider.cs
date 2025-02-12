using Blogifier.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blogifier.Files.Providers
{
    public interface IFileDescriptorProvider
    {
        Task<IEnumerable<FileDescriptor>> GetPagedAsync(InfinitePagingDescriptor pagingDescriptor, string searchTerm);
        Task<IEnumerable<FileDescriptor>> GetPagedAsync(PagingDescriptor pagingDescriptor, string searchTerm);
        Task<FileDescriptor> GetAsync(Guid id);
        Task<FileDescriptor> GetAsync(string filePath);
        Task<bool> InsertAsync(FileDescriptor fileDescriptor);
        Task<bool> UpdateAsync(FileDescriptor fileDescriptor);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(string filePath);
        Task<IEnumerable<FileDescriptor>> ListAsync();
    }
}
