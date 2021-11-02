using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;

namespace Blogifier.Core.Providers
{
    public interface IAuthorProvider
	{
		Task<List<Author>> GetAuthors();
        Task<Author> GetAuthorAsync(Guid id);
		Task<Author> FindByEmail(string email);
		Task<bool> Verify(LoginModel model);
		Task<bool> Register(RegisterModel model);
		Task<bool> Add(Author author);
		Task<bool> Update(Author author);
		Task<bool> ChangePassword(RegisterModel model);
		Task<bool> Remove(Guid id);
	}
}
