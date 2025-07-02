namespace BoothWatcher
{
    public class Booth
    {
        private static HttpClient? client { get; set; }
        public Booth()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                UseCookies = false
            };
            client = new(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.80 Safari/537.36 Edg/98.0.1108.50");
            client.DefaultRequestHeaders.Add("cookie", "adult=t");
        }
        public async Task<List<BoothItem>> GetNewBoothItemAsync()
        {
            List<BoothItem> items = new();
            if(client is null) throw new ArgumentNullException("client is null");
            HttpResponseMessage? response = await client.GetAsync("https://booth.pm/en/browse/3D%20Models?adult=include&sort=new");
            try
            {
                string content = await response.Content.ReadAsStringAsync();
                content = content.Replace("<", "\r\n<");
                string[] itemcards = content.Split("<li class=\"item-card");
                foreach (string itemcard in itemcards)
                {
                    BoothItem item = new();
                    StringReader reader = new(itemcard);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("data-product-id="))
                            item.Id = line.Split("data-product-id=\"", StringSplitOptions.None)[1].Split('"')[0];
                        if (line.Contains("data-original"))
                            item.thumbnailImageUrls.Add(line.Split("data-original=\"", StringSplitOptions.None)[1]
                                .Split('"')[0]);
                        if (line.Contains("item-card__title-anchor--multiline nav"))
                            item.Title =
                                line.Split("item-card__title-anchor--multiline nav !min-h-[auto]\"", StringSplitOptions.None)[1]
                                    .Split('>')[1];
                        if (line.Contains("price u-text-primary u-text-left u-tpg-caption2\">"))
                            item.Price = line.Split("price u-text-primary u-text-left u-tpg-caption2\">",
                                StringSplitOptions.None)[1];
                        if (line.Contains("class=\"item-card__shop-name\">"))
                            item.ShopName = line.Split("class=\"item-card__shop-name\">")[1];
                        if (line.Contains("item-card__shop-name-anchor nav"))
                            item.ShopUrl =
                                line.Split("item-card__shop-name-anchor nav", StringSplitOptions.None)[1]
                                    .Split("href=\"")[1].Split('"')[0];
                        if (line.Contains("user-avatar"))
                            item.ShopImageUrl =
                                line.Split("user-avatar", StringSplitOptions.None)[1]
                                    .Split("src=\"", StringSplitOptions.None)[1].Split('"')[0];
                    }

                    items.Add(item);
                }

                items.RemoveAt(0);
                return items;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"exception at method GetNewBoothItemAsync {exception}");
            }
            items.RemoveAt(0);
            return items;
        }
    }
}