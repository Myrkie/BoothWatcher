using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

#pragma warning disable IDE0036
#pragma warning disable IDE0090

namespace BoothWatcher
{
    public class Booth
    {
        private static HttpClient Client;
        public Booth()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                UseCookies = false
            };
            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.80 Safari/537.36 Edg/98.0.1108.50");
            Client.DefaultRequestHeaders.Add("Cookie", "adult=t");
        }

        //this is going to hurt a lot of people lol but I'm lazy
        //tbh it works, Ill look into learning HTML Agility pack later
        // TODO Possibly change the below async task to use HTML Agility instead of String Split
        public async Task<List<BoothItem>> GetNewBoothItemAsync()
        {
            List<BoothItem> items = new();
            HttpResponseMessage response = await Client.GetAsync($"https://booth.pm/en/browse/3D%20Models?{(Program.Config.AdultFilter ? "adult=include&" : "")}sort=new");
            string content = await response.Content.ReadAsStringAsync();
            content = content.Replace("<", "\r\n<");
            string[] itemcards = content.Split("<li class=\"item-card");
            foreach (string itemcard in itemcards) 
            {
                BoothItem item = new BoothItem();
                StringReader reader = new StringReader(itemcard);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("data-product-id="))
                    {
                        item.ID = line.Split("data-product-id=\"", StringSplitOptions.None)[1].Split('"')[0];
                    }
                    if (line.Contains("data-original"))
                    {
                        item.ThumbnailURLs.Add(line.Split("data-original=\"", StringSplitOptions.None)[1].Split('"')[0]);
                    }
                    if (line.Contains("item-card__title-anchor--multiline nav"))
                    {
                        item.Title = line.Split("item-card__title-anchor--multiline nav\"", StringSplitOptions.None)[1].Split('>')[1];
                    }
                    if (line.Contains("price u-text-primary u-text-left u-tpg-caption2\">"))
                    {
                        item.Price = line.Split("price u-text-primary u-text-left u-tpg-caption2\">", StringSplitOptions.None)[1];
                    }
                    if (line.Contains("class=\"item-card__shop-name\">"))
                    {
                        item.ShopName = line.Split("class=\"item-card__shop-name\">")[1];
                    }
                    if (line.Contains("item-card__shop-name-anchor"))
                    {
                        item.ShopURL = line.Split("item-card__shop-name-anchor", StringSplitOptions.None)[1].Split("href=\"")[1].Split('"')[0];
                    }
                    if (line.Contains("user-avatar"))
                    {
                        item.ShopImageURL = line.Split("user-avatar", StringSplitOptions.None)[1].Split("src=\"", StringSplitOptions.None)[1].Split('"')[0];
                    }
                }
                items.Add(item);
            }
            items.RemoveAt(0);
            return items;
        }
    }
}