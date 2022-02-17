using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoothWatcher
{
    public class BoothItem
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? price { get; set; }
        public List<string> thumbnailImageUrls = new List<string>();
        public string? shopName { get; set; }
        public string? shopUrl { get; set; }
        public string? shopImageUrl { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ID: {id}");
            sb.AppendLine($"Title: {title}");
            sb.AppendLine($"Price: {price}");
            sb.AppendLine($"Thumbnail Image URL: {thumbnailImageUrls}");
            sb.AppendLine($"Shop Name: {shopName}");
            sb.AppendLine($"Shop URL: {shopUrl}");
            sb.AppendLine($"Shop Image URL: {shopImageUrl}");
            return sb.ToString();
        }
    }
}
