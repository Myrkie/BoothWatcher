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
        private static bool _firstartup = true;
        
        // TODO make every Static in the region below into a json config
        #region Static Properties
        
        private static string _StartupMessage = "Starting Up";
        private static string _Username = "BoothWatcherV1.6";
        private static string _AvatarURL = "https://i.imgur.com/gEJk8uX.jpg";
        private static string _FooterIconAvatar = "https://i.imgur.com/gEJk8uX.jpg";
        private static string _FooterText = "Made by Keafy";
        private static string _webhook = "Webhooks.txt";
        private static bool _tts = false;
        
        #endregion

        static void Main(string[] args)
        {
            if (File.Exists("AlreadyAddedId.txt"))
                foreach (string id in File.ReadAllLines("AlreadyAddedId.txt"))
                    AlreadyAddedID.Add(id);
            if (!File.Exists(_webhook))
            {
                using (FileStream fs = File.Create(_webhook))
                    Console.WriteLine("Paste in your webhook URL'(s) here");

                #region URLParse
                
                string _userinput = Console.ReadLine();
                
                string urls = String.Join("\nhttp", _userinput.Split("http"));

                if (urls.StartsWith("\n"))
                {
                    urls = urls.Substring(1);
                }
                File.AppendAllText(_webhook, urls);
                
                Startloop();
                
                #endregion
            }
            else
            {
                Startloop();
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
            
            BoothWatcherTimer.Elapsed += BoothWatcher_Elapsed;
            DiscordWebhook.Elapsed += DiscordWebhook_Elapsed;
            BoothWatcherTimer.Start();
            DiscordWebhook.Start();
            
            BoothWatcher_Elapsed();
            Thread.Sleep(-1);
        }

        private static void Startloop()
        {
            Console.WriteLine("Starting With: " + CountLinesInTextFile(_webhook) + " Webhook Connections");
            foreach (string webhook in File.ReadAllLines(_webhook))
            {
                clients.Add(new DiscordWebhookClient(webhook));
            }
        }

        static int CountLinesInTextFile(string f)
        {
            var count = 0;
            using StreamReader r = new StreamReader(f);
            while (r.ReadLine() != null)
            {
                count++;
            }

            return count;
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
                                                       author: new DiscordMessageEmbedAuthor(item.shopName, item.shopUrl, item.shopImageUrl),
                                                       url: $"https://booth.pm/en/items/{item.id}",
                                                       fields: new[]
                                                       {
                                                           new DiscordMessageEmbedField("Price:", item.price),
                                                           new DiscordMessageEmbedField("Booth ID:", item.id)
                                                       },
                                                       image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[0]),
                                                       footer: new DiscordMessageEmbedFooter(_FooterText, _FooterIconAvatar)));
                    for (int i = 1; i < 4 && i < item.thumbnailImageUrls.Count; i++)
                        embeds.Add(new DiscordMessageEmbed(url: $"https://booth.pm/en/items/{item.id}", image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[i])));
                }
                DiscordMessage? message = new(username: _Username, avatarUrl: _AvatarURL, tts: _tts, embeds: embeds.ToArray());
                clients.ForEach(client =>
                {
                    if (_firstartup)
                    {
                        _firstartup = false;
                        clients.ForEach(client =>
                        {
                            DiscordMessage? _init = new(_StartupMessage, username: _Username, avatarUrl: _AvatarURL, tts: _tts);
                            client.SendToDiscord(_init);
                            Thread.Sleep(4000);
                        });
                    }
                    client.SendToDiscord(message);
                });
                Console.WriteLine($"{item.title} Has been Sent!");
            }
        }

        private static async void BoothWatcher_Elapsed(object? sender = null, System.Timers.ElapsedEventArgs? e = null)
        {
            int newitemscount = 0;
            List<BoothItem>? boothitems = await watcher.GetNewBoothItemAsync();
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
            {
                if (_firstartup)
                {
                    _firstartup = false;
                    clients.ForEach(client =>
                    { 
                        DiscordMessage? _init = new(_StartupMessage, username: _Username, avatarUrl: _AvatarURL, tts: _tts);
                        client.SendToDiscord(_init);
                    });
                }
                Console.WriteLine("No items added to queue");
            }
        }
    }
}