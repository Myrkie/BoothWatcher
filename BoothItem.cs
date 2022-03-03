using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoothWatcher
{
    public class BoothItem
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Price { get; set; }
        public List<string> thumbnailImageUrls = new List<string>();
        public string? ShopName { get; set; }
        public string? ShopUrl { get; set; }
        public string? ShopImageUrl { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ID: {Id}");
            sb.AppendLine($"Title: {Title}");
            sb.AppendLine($"Price: {Price}");
            sb.AppendLine($"Thumbnail Image URL: {thumbnailImageUrls}");
            sb.AppendLine($"Shop Name: {ShopName}");
            sb.AppendLine($"Shop URL: {ShopUrl}");
            sb.AppendLine($"Shop Image URL: {ShopImageUrl}");
            return sb.ToString();
        }
    }
}
