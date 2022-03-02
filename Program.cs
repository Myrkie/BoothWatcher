using JNogueira.Discord.Webhook.Client;
using DeeplTranslator = Deepl.Deepl; 
using Language = Deepl.Deepl.Language;


namespace BoothWatcher
{
    internal class Program
    {
        static Booth _watcher = new();
        static Queue<BoothItem> _items = new();
        static List<DiscordWebhookClient> _clients = new();
        static HashSet<string> _alreadyAddedId = new();
        private static bool _firstartup = true;
        
        // TODO make every Static in the region below into a json config
        #region Static Properties
        
        private static string _startupMessage = "Starting Up";
        private static string _username = $"BoothWatcher - V{typeof(Program).Assembly.GetName().Version}";
        private static string _avatarUrl = "https://i.imgur.com/gEJk8uX.jpg";
        private static string _footerIconAvatar = "https://i.imgur.com/gEJk8uX.jpg";
        private static string _footerText = "Made by Keafy & Myrkur";
        private static string _webhook = "Webhooks.txt";
        private static string _watchlist = "WatchList.txt";
        private static string _blacklist = "BlackList.txt";
        private static string _alreadyadded = "AlreadyAddedId.txt";
        private static bool _tts = false;
        
        #endregion

        static void Main(string[] args)
        {
            if (!File.Exists(_watchlist)) File.Create(_watchlist);
            if (!File.Exists(_blacklist)) File.Create(_blacklist);
            if (File.Exists(_alreadyadded))
                foreach (string id in File.ReadAllLines(_alreadyadded))
                    _alreadyAddedId.Add(id);
            if (!File.Exists(_webhook))
            {
                using (FileStream fs = File.Create(_webhook))
                    Console.WriteLine("Paste in your webhook URL'(s) here");

                #region URLParse
                
                string userinput = Console.ReadLine();
                
                string urls = String.Join("\nhttp", userinput.Split("http"));

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

            System.Timers.Timer boothWatcherTimer = new(60 * 1000)
            {
                AutoReset = true,
                Enabled = true
            };
            System.Timers.Timer discordWebhook = new(5000)
            {
                AutoReset = true,
                Enabled = true
            };
            
            boothWatcherTimer.Elapsed += BoothWatcher_Elapsed;
            discordWebhook.Elapsed += DiscordWebhook_Elapsed;
            boothWatcherTimer.Start();
            discordWebhook.Start();
            
            BoothWatcher_Elapsed();
            Thread.Sleep(-1);
        }

        private static void Startloop()
        {
            Console.WriteLine("Starting With: " + CountLinesInTextFile(_webhook) + " Webhook Connections");
            foreach (string webhook in File.ReadAllLines(_webhook))
            {
                _clients.Add(new DiscordWebhookClient(webhook));
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
            if (_items.Count != 0)
            {
                BoothItem? item = _items.Dequeue();
                List<DiscordMessageEmbed> embeds = new();
                if (!File.ReadAllText(_blacklist).Contains(item.shopUrl) && item.thumbnailImageUrls.Count > 0)
                {
                    embeds.Add(new DiscordMessageEmbed(item.title, color: 16711807,
                        author: new DiscordMessageEmbedAuthor(item.shopName, item.shopUrl, item.shopImageUrl),
                        url: $"https://booth.pm/en/items/{item.id}",
                        fields: new[]
                        {
                            new DiscordMessageEmbedField("Price:", item.price),
                            new DiscordMessageEmbedField("Booth ID:", item.id),
                            new DiscordMessageEmbedField("Translated Title: ", TranslateText(item.title))
                        },
                        image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[0]),
                        footer: new DiscordMessageEmbedFooter(FooterText, _footerIconAvatar)));
                    for (int i = 1; i < 4 && i < item.thumbnailImageUrls.Count; i++)
                        embeds.Add(new DiscordMessageEmbed(url: $"https://booth.pm/en/items/{item.id}", image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[i])));
                }
                DiscordMessage? message = new(username: _username, avatarUrl: _avatarUrl, tts: _tts, embeds: embeds.ToArray());
                _clients.ForEach(client =>
                {
                    StartupCheck();
                    CheckWatchList(item);
                    Thread.Sleep(1000);
                    client.SendToDiscord(message);
                });
                if (!File.ReadAllText(_blacklist).Contains(item.shopUrl))
                    Console.WriteLine($"{item.title} Has been Sent!");
            }
        }

        private static string TranslateText(string input)
        {
            var translate = new DeeplTranslator(selectedLanguage: Language.JP, targetLanguage: Language.EN, input);
            return translate.Resp;
        }

        private static async void BoothWatcher_Elapsed(object? sender = null, System.Timers.ElapsedEventArgs? e = null)
        {
            try
            {
                int newitemscount = 0;
                List<BoothItem>? boothitems = await _watcher.GetNewBoothItemAsync();
                foreach (var item in boothitems)
                {
                    if (!_alreadyAddedId.Contains(item.id))
                    {
                        File.AppendAllText(_alreadyadded, item.id + Environment.NewLine);
                        _alreadyAddedId.Add(item.id);
                        _items.Enqueue(item);
                        if (!File.ReadAllText(_blacklist).Contains(item.shopUrl))
                        {
                            newitemscount++;
                        }
                        else
                        {
                            Console.WriteLine($"Blacklisted items from author removed from queue");
                        }
                    }
                }
                if (newitemscount > 0)
                    Console.WriteLine($"Added {newitemscount} to queue");
                else
                {
                    StartupCheck();
                    Console.WriteLine("No items added to queue");
                }
            }
            catch { }
        }

        private static void StartupCheck()
        {
            if (!_firstartup) return;
            {
                _firstartup = false;
                _clients.ForEach(client =>
                {
                    DiscordMessage? init = new(_startupMessage, username: _username, avatarUrl: _avatarUrl, tts: _tts);
                    client.SendToDiscord(init);
                    Thread.Sleep(4000);
                });
            }
        }

        private static void CheckWatchList(BoothItem item)
        {
            if (File.ReadAllText(_watchlist).Contains(item.shopUrl))
            {
                _clients.ForEach(client =>
                { 
                    DiscordMessage? watchlistitem = new($":arrow_down:  Post by author is on watchlist  :arrow_down: // <{item.shopUrl}>", username: _username, avatarUrl: _avatarUrl, tts: _tts);
                    client.SendToDiscord(watchlistitem);
                });
            }
        }
    }
}