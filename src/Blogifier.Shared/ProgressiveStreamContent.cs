using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blogifier.Shared
{
    public class ProgressiveStreamContent : StreamContent
    {
        public event Action<long, double> OnProgress;
        private readonly System.IO.Stream _stream;
        private readonly int _maxBuffer = 1024 * 4;

        public ProgressiveStreamContent(System.IO.Stream stream, int maxBuffer, Action<long, double> onProgress) : base(stream)
        {
            _stream = stream;
            _maxBuffer = maxBuffer;
            OnProgress += onProgress;
        }

        protected async override Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context)
        {
            var buffer = new byte[_maxBuffer];
            var totalLength = _stream.Length;
            var uploadedByteCount = 0L;
            var lastPercentage = 0;

            using (_stream)
            {
                while (true)
                {
                    var length = await _stream.ReadAsync(buffer, 0, _maxBuffer);

                    if (length <= 0)
                    {
                        break;
                    }

                    uploadedByteCount += length;
                    var percentage = (int)(uploadedByteCount * 100 / _stream.Length);

                    await stream.WriteAsync(buffer);

                    if(percentage != lastPercentage && percentage != 100)
                    {
                        OnProgress?.Invoke(uploadedByteCount, percentage);
                        lastPercentage = percentage;

                        await Task.Yield();
                    }
                }

                OnProgress?.Invoke(uploadedByteCount, 100);
            }
        }
    }
}
