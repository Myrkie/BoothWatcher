using System.Text;
using System.Collections.Generic;

#pragma warning disable IDE0090

namespace BoothWatcher
{
    public class BoothItem
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string Price { get; set; }
        public List<string> ThumbnailURLs = new List<string>();
        public string ShopName { get; set; }
        public string ShopURL { get; set; }
        public string ShopImageURL { get; set; }
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"ID: {ID}");
            builder.AppendLine($"Title: {Title}");
            builder.AppendLine($"Price: {Price}");
            builder.AppendLine($"Thumbnail Image URL: {ThumbnailURLs}");
            builder.AppendLine($"Shop Name: {ShopName}");
            builder.AppendLine($"Shop URL: {ShopURL}");
            builder.AppendLine($"Shop Image URL: {ShopImageURL}");
            return builder.ToString();
        }
    }
}
