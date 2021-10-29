using Blogifier.Shared;
using Blogifier.Shared.Extensions;
using Blogifier.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers
{
	public class StorageProvider : IStorageProvider
	{
		private string _storageRoot;
		private readonly string _slash = Path.DirectorySeparatorChar.ToString();

		public StorageProvider()
		{
			_storageRoot = $"{FileSystemHelpers.ContentRoot}{_slash}wwwroot{_slash}data{_slash}";
		}

		public bool FileExists(string path)
		{
			Serilog.Log.Information($"File exists: {Path.Combine(FileSystemHelpers.ContentRoot, path)}");
			return File.Exists(Path.Combine(FileSystemHelpers.ContentRoot, path));
		}

		public async Task<IList<string>> GetThemes()
		{
			var themes = new List<string>();
			var themesDirectory = Path.Combine(FileSystemHelpers.ContentRoot, "Views", "Themes");
			try
			{
				foreach (string dir in Directory.GetDirectories(themesDirectory))
				{
					themes.Add(Path.GetFileName(dir));
				}
			}
			catch { }
			return await Task.FromResult(themes);
		}

		public async Task<ThemeSettings> GetThemeSettings(string theme)
		{
			var settings = new ThemeSettings();
			var fileName = Path.Combine(FileSystemHelpers.ContentRoot, $"wwwroot{_slash}themes{_slash}{theme.ToLower()}{_slash}settings.json");
			if (File.Exists(fileName))
			{
				try
				{
					string jsonString = File.ReadAllText(fileName);
					settings = JsonSerializer.Deserialize<ThemeSettings>(jsonString);
				}
				catch (Exception ex)
				{
					Serilog.Log.Error($"Error reading theme settings: {ex.Message}");
					return null;
				}
			}

			return await Task.FromResult(settings);
		}

		public async Task<bool> SaveThemeSettings(string theme, ThemeSettings settings)
		{
			var fileName = Path.Combine(FileSystemHelpers.ContentRoot, $"wwwroot{_slash}themes{_slash}{theme.ToLower()}{_slash}settings.json");
			try
			{
				if (File.Exists(fileName))
					File.Delete(fileName);

				var options = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true	};

				string jsonString = JsonSerializer.Serialize(settings, options);

				using FileStream createStream = File.Create(fileName);
				await JsonSerializer.SerializeAsync(createStream, settings, options);
			}
			catch (Exception ex)
			{
				Serilog.Log.Error($"Error writing theme settings: {ex.Message}");
				return false;
			}
			return true;
		}

		public async Task<string> UploadFromWeb(Uri requestUri, string root, string path = "")
		{
			path = path.Replace("/", _slash);
			VerifyPath(path);

			var fileName = TitleFromUri(requestUri);
			var filePath = string.IsNullOrEmpty(path) ?
				 Path.Combine(_storageRoot, fileName) :
				 Path.Combine(_storageRoot, path + _slash + fileName);

			using (WebClient client = new WebClient())
			{
				client.DownloadFile(requestUri, filePath);
				return await Task.FromResult($"![{fileName}]({root}{PathToUrl(filePath)})");
			}
		}

		public async Task<string> UploadBase64Image(string baseImg, string root, string path = "")
		{
			path = path.Replace("/", _slash);
			var fileName = "";

			VerifyPath(path);
			string imgSrc = GetImgSrcValue(baseImg);

			Random rnd = new Random();

			if (imgSrc.StartsWith("data:image/png;base64,"))
			{
				fileName = string.Format("{0}.png", rnd.Next(1000, 9999));
				imgSrc = imgSrc.Replace("data:image/png;base64,", "");
			}
			if (imgSrc.StartsWith("data:image/jpeg;base64,"))
			{
				fileName = string.Format("{0}.jpeg", rnd.Next(1000, 9999));
				imgSrc = imgSrc.Replace("data:image/jpeg;base64,", "");
			}
			if (imgSrc.StartsWith("data:image/gif;base64,"))
			{
				fileName = string.Format("{0}.gif", rnd.Next(1000, 9999));
				imgSrc = imgSrc.Replace("data:image/gif;base64,", "");
			}

			var filePath = string.IsNullOrEmpty(path) ?
				 Path.Combine(_storageRoot, fileName) :
				 Path.Combine(_storageRoot, path + _slash + fileName);

			await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(imgSrc));

			return $"![{fileName}]({root}{PathToUrl(filePath)})";
		}

		#region Private members

		void VerifyPath(string path)
		{
			path = path.SanitizePath();

			if (!string.IsNullOrEmpty(path))
			{
				var dir = Path.Combine(_storageRoot, path);

				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
			}
		}

		string TitleFromUri(Uri uri)
		{
			var title = uri.ToString().ToLower();
			title = title.Replace("%2f", "/");

			if (title.EndsWith(".axdx"))
			{
				title = title.Replace(".axdx", "");
			}
			if (title.Contains("image.axd?picture="))
			{
				title = title.Substring(title.IndexOf("image.axd?picture=") + 18);
			}
			if (title.Contains("file.axd?file="))
			{
				title = title.Substring(title.IndexOf("file.axd?file=") + 14);
			}
			if (title.Contains("encrypted-tbn") || title.Contains("base64,"))
			{
				Random rnd = new Random();
				title = string.Format("{0}.png", rnd.Next(1000, 9999));
			}

			if (title.Contains("/"))
			{
				title = title.Substring(title.LastIndexOf("/"));
			}

			title = title.Replace(" ", "-");

			return title.Replace("/", "").SanitizeFileName();
		}

		string PathToUrl(string path)
		{
			string url = path.ReplaceIgnoreCase(_storageRoot, "").Replace(_slash, "/");
			return $"data/{url}";
		}

		string GetImgSrcValue(string imgTag)
		{
			if (!(imgTag.Contains("data:image") && imgTag.Contains("src=")))
				return imgTag;

			int start = imgTag.IndexOf("src=");
			int srcStart = imgTag.IndexOf("\"", start) + 1;

			if (srcStart < 2)
				return imgTag;

			int srcEnd = imgTag.IndexOf("\"", srcStart);

			if (srcEnd < 1 || srcEnd <= srcStart)
				return imgTag;

			return imgTag.Substring(srcStart, srcEnd - srcStart);
		}

		#endregion
	}
}
