using System;

namespace Blogifier.Shared
{
	public class Newsletter
	{
        public Newsletter()
        {
            Id = Guid.NewGuid();
        }

		public Guid Id { get; set; }
		public Guid PostId { get; set; }
		public bool Success { get; set; }

		public DateTime DateCreated { get; set; }
		public DateTime DateUpdated { get; set; }

		public Post Post { get; set; }
	}
}
