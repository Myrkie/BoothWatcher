using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoothWatcher
{
    public class BoothItem
    {
        public string Id { get => id; set => id = value; }
        public string Title { get => title; set => title = value; }
        public string Price { get => price; set => price = value; }
        public List<string> thumbnailImageUrls = new List<string>();

        public string ShopName { get => shopName; set => shopName = value; }
        public string ShopUrl { get => shopUrl; set => shopUrl = value; }
        public string ShopImageUrl { get => shopImageUrl; set => shopImageUrl = value; }
        private string shopImageUrl = "";
        private string shopUrl = "";
        private string shopName = "";
        private string price = "";
        private string title = "";
        private string id = "";
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
