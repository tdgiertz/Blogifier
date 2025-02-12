using System;
using System.Linq.Expressions;

namespace Blogifier.Core.Providers.MongoDb.Models
{
    internal class SortDefinition<T>
    {
        public Expression<Func<T, object>> Sort { get; set; }
        public bool IsDescending { get; set; }
    }
}
