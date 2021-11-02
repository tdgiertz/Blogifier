using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Blogifier.Admin.Models
{
    public class EditorOptions
    {
        [JsonPropertyName("toolbarItems")]
        public IEnumerable<ToolbarItem> ToolbarItems { get; set; }
    }
}
