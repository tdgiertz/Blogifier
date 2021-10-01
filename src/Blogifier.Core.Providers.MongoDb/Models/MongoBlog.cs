using System;
using System.Collections.Generic;
using Blogifier.Shared;

namespace Blogifier.Core.Providers.MongoDb.Models
{
    internal class MongoBlog : Blog
    {
        public List<Guid> AuthorIds { get; set; }
    }
}
