using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blogifier.Shared;
using Microsoft.AspNetCore.Http;

namespace Blogifier.Core.Providers
{
    public interface IStorageProvider
	{
		Task<IList<string>> GetThemes();
		bool FileExists(string path);
		Task<string> UploadFromWeb(Uri requestUri, string root, string path = "");
		Task<string> UploadBase64Image(string baseImg, string root, string path = "");
		Task<ThemeSettings> GetThemeSettings(string theme);
		Task<bool> SaveThemeSettings(string theme, ThemeSettings settings);
	}
}
