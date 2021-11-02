using System.Text.Json.Serialization;
using Blogifier.Admin.Components;
using Microsoft.JSInterop;

namespace Blogifier.Admin.Models
{
    public class ToolbarItem
    {
        [JsonPropertyName("reference")]
        public DotNetObjectReference<ToolbarItem> Reference { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("icon")]
        public string Icon { get; set; }
        [JsonPropertyName("callback")]
        public JsCallback Callback { get; set; }
        [JsonPropertyName("canDisable")]
        public bool CanDisable { get; set; } = true;
        [JsonPropertyName("className")]
        public string ClassName { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool CanHide { get; set; }

        public ToolbarItem()
        {
            Reference = DotNetObjectReference.Create(this);
        }
    }
}
