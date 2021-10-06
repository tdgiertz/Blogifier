using System.Collections.Generic;

namespace Blogifier.Shared
{
    public class FileListModel
    {
        public IEnumerable<FileModel> FileModels { get; set; }
        public Pager Pager { get; set; }
    }
}
