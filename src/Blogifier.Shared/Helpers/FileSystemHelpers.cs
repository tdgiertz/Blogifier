using System.IO;
using Blogifier.Shared.Extensions;

namespace Blogifier.Shared.Helpers
{
    public static class FileSystemHelpers
    {
		public static string ContentRoot
		{
			get
			{
                var slash = Path.DirectorySeparatorChar;
				var path = Directory.GetCurrentDirectory();
				var testsDirectory = $"tests{slash}Blogifier.Tests";
				var appDirectory = $"src{slash}Blogifier";

				// development unit test run
				if (path.LastIndexOf(testsDirectory) > 0)
				{
					path = path.Substring(0, path.LastIndexOf(testsDirectory));
					return $"{path}src{slash}Blogifier";
				}

				// development debug run
				if (path.LastIndexOf(appDirectory) > 0)
				{
					path = path.Substring(0, path.LastIndexOf(appDirectory));
					return $"{path}src{slash}Blogifier";
				}
				return path;
			}
		}

		public static string GetFileName(string fileName)
		{
            var slash = Path.DirectorySeparatorChar;

			// some browsers pass uploaded file name as short file name
			// and others include the path; remove path part if needed
			if (fileName.Contains(slash))
			{
				fileName = fileName.Substring(fileName.LastIndexOf(slash));
				fileName = fileName.Replace($"{slash}", "");
			}
			// when drag-and-drop or copy image to TinyMce editor
			// it uses "mceclip0" as file name; randomize it for multiple uploads
			if (fileName.StartsWith("mceclip0"))
			{
				var rnd = new System.Random();
				fileName = fileName.Replace("mceclip0", rnd.Next(100000, 999999).ToString());
			}

			return fileName.SanitizePath();
		}

    }
}
