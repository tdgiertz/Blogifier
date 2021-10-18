using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Blogifier.Shared
{
    public static class IAsyncEnumerableExtensions
    {
        public static async Task AsyncParallelForEach<T>(this IAsyncEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var block = new ActionBlock<T>(body, options);

            await foreach (var item in source)
            {
                block.Post(item);
            }

            block.Complete();
            await block.Completion;
        }
    }
}
