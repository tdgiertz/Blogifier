using System;
using System.Collections.Generic;

namespace Blogifier.Shared
{
    public class FileSyncronizationModel
    {
        public IEnumerable<SyncronizationModel> ImportFileModels {get;set;}
        public IEnumerable<SyncronizationModel> RemoveFileModels {get;set;}
    }
    public class SyncronizationModel
    {
        public string Filename {get;set;}
        public string MimeType {get;set;}
        public string Url {get;set;}
        public string Description {get;set;}
        public DateTime DateCreated {get;set;}
    }
}
