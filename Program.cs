using System;
using JNogueira.Discord.Webhook.Client;

namespace BoothWatcher
{
    internal class Program
    {
        static Booth watcher = new();
        static Queue<BoothItem> items = new();
        static List<DiscordWebhookClient> clients = new();
        static HashSet<string> AlreadyAddedID = new();

        static void Main(string[] args)
        {
            if (File.Exists("AlreadyAddedId.txt"))
                foreach (string id in File.ReadAllLines("AlreadyAddedId.txt"))
                    AlreadyAddedID.Add(id);
            if (!File.Exists("Webhooks.txt"))
            {
                Console.WriteLine("Please create \"Webhooks.txt\" and add webhook urls to file");
                Thread.Sleep(-1);
            }
            else
            {
                foreach (string webhook in File.ReadAllLines("Webhooks.txt"))
                {
                    clients.Add(new DiscordWebhookClient(webhook));
                }
            }

            System.Timers.Timer BoothWatcherTimer = new(60 * 1000)
            {
                AutoReset = true,
                Enabled = true
            };
            System.Timers.Timer DiscordWebhook = new(5000)
            {
                AutoReset = true,
                Enabled = true
            };
            
            BoothWatcherTimer.Elapsed += BothWatcher_Elapsed;
            DiscordWebhook.Elapsed += DiscordWebhook_Elapsed;
            BoothWatcherTimer.Start();
            DiscordWebhook.Start();
            
            BothWatcher_Elapsed();
            Thread.Sleep(-1);
        }

        private static void DiscordWebhook_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (items.Count != 0)
            {
                BoothItem? item = items.Dequeue();
                List<DiscordMessageEmbed> embeds = new();
                if (item.thumbnailImageUrls.Count > 0)
                {
                    embeds.Add(new DiscordMessageEmbed(item.title,
                                                       color: 16711807,
                                                       author: new DiscordMessageEmbedAuthor(item.shopName,
                                                                                             item.shopUrl,
                                                                                             item.shopImageUrl),
                                                       url: $"https://booth.pm/en/items/{item.id}",
                                                       fields: new[]
                                                       {
                                                           new DiscordMessageEmbedField("Price:",
                                                           item.price)
                                                       },
                                                       image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[0]),
                                                       footer: new DiscordMessageEmbedFooter("Made by Keafy",
                                                                                             "https://i.imgur.com/gEJk8uX.jpg")));
                    for (int i = 1; i < 4 && i < item.thumbnailImageUrls.Count; i++)
                        embeds.Add(new DiscordMessageEmbed(url: $"https://booth.pm/en/items/{item.id}",
                                                           image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[i])));
                }
                DiscordMessage? message = new(username: "BoothWatcher",
                                                             avatarUrl: "https://i.imgur.com/gEJk8uX.jpg",
                                                             tts: false,
                                                             embeds: embeds.ToArray());
                clients.ForEach(client =>
                {
                    client.SendToDiscord(message);
                });
                Console.WriteLine($"{item.title} Has been Sent!");
            }
        }

        private static async void BothWatcher_Elapsed(object? sender = null, System.Timers.ElapsedEventArgs? e = null)
        {
            int newitemscount = 0;
            List<BoothItem>? boothitems = await watcher.GetNewBothItemAsync();
            foreach (var item in boothitems)
            {
                if (!AlreadyAddedID.Contains(item.id))
                {
                    File.AppendAllText("AlreadyAddedId.txt", item.id + Environment.NewLine);
                    AlreadyAddedID.Add(item.id);
                    items.Enqueue(item);
                    newitemscount++;
                }
            }
            if (newitemscount > 0)
                Console.WriteLine($"Added {newitemscount} to queue");
            else
                Console.WriteLine("No items added to queue");
        }
    }
}