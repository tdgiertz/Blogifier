using System.Threading.Tasks;
using System.IO;
using ImageMagick;

namespace Blogifier.Files
{
    public class ImageProcessor
    {
        public static async Task<Stream> ResizeImageAsync(Stream stream, int width = 100, int height = 100)
        {
            var result = new MemoryStream();

            using (var image = new MagickImage(stream))
            {
                image.Thumbnail(new MagickGeometry(width, height));
                await image.WriteAsync(result);
            }

            return result;
        }
    }
}
