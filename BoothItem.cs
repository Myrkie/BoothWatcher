using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoothWatcher
{
    public class BoothItem
    {
        public List<string> thumbnailImageUrls = new List<string>();
        public string ShopImageUrl = "";
        public string ShopUrl = "";
        public string ShopName = "";
        public string Price = "";
        public string Title = "";
        public string Id = "";

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
