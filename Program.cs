using System.Net;
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
        private static DeeplTranslator translate;
        private static bool _firstartup = true;

        static void Main(string[] args)
        {
            Console.WriteLine("started file watcher");
            using var fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileSystemWatcher.Path = Directory.GetCurrentDirectory();
            fileSystemWatcher.Filter = JsonConfig._configpath;
            fileSystemWatcher.Changed += OnChanged;

            JsonConfig.Configure.load();
            if (File.Exists(JsonConfig._config._alreadyadded))
                foreach (string id in File.ReadAllLines(JsonConfig._config._alreadyadded))
                    _alreadyAddedId.Add(id);
            bool isEmpty = IsEmpty(JsonConfig._config._webhook);
            if (isEmpty)
            {
                Console.WriteLine("Paste in your webhook URLs here");

                #region URLParse

                bool doLoop = true;
                while (doLoop)
                {
                    string userinput = Console.ReadLine();
                    if (string.IsNullOrEmpty(userinput))
                    {
                        doLoop = false;
                    }
                    else
                    {
                        JsonConfig._config._webhook.Add(userinput);
                        JsonConfig.Configure.forcesave();
                        Console.WriteLine("Added, leave blank to finish");
                    }
                }
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

            if (!string.IsNullOrWhiteSpace(JsonConfig._config._proxyHost))
            {
                Proxyhandler.DownloadFreeProxies();
                
                System.Timers.Timer proxyrotation = new(60 * 5000)
                {
                    AutoReset = true,
                    Enabled = true
                };
                
                proxyrotation.Elapsed += Proxyhandler.ResetProxies;
                proxyrotation.Start();
            }
            
            boothWatcherTimer.Elapsed += BoothWatcher_Elapsed;
            discordWebhook.Elapsed += DiscordWebhook_Elapsed;
            boothWatcherTimer.Start();
            discordWebhook.Start();
            
            BoothWatcher_Elapsed();
            fileSystemWatcher.EnableRaisingEvents = true;
            Thread.Sleep(-1);
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed) return;

            Console.WriteLine("Config file changed, reloading");
            Thread.Sleep(5000);
            JsonConfig.Configure.load();
        }

        private static void Startloop()
        {
            Console.WriteLine("Starting With: " + JsonConfig._config._webhook.Count + " Webhook Connections");
            foreach (string webhook in JsonConfig._config._webhook)
            {
                _clients.Add(new DiscordWebhookClient(webhook));
            }
        }

        private static void DiscordWebhook_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_items.Count != 0)
            {
                BoothItem? item = _items.Dequeue();
                List<DiscordMessageEmbed> embeds = new();

                bool containsBlacklisted = JsonConfig._config._keywordblacklist.Any(item.Title.ToLower().Contains) ||
                                            JsonConfig._config._blacklist.Contains(item.ShopUrl);
                if (!containsBlacklisted && item.thumbnailImageUrls.Count > 0)
                {
                    embeds.Add(new DiscordMessageEmbed(item.Title, color: 16711807,
                        author: new DiscordMessageEmbedAuthor(item.ShopName, item.ShopUrl, item.ShopImageUrl),
                        url: $"https://booth.pm/en/items/{item.Id}",
                        fields: new[]
                        {
                            new DiscordMessageEmbedField("ScrapeTime: ", $"<t:{DateTimeOffset.Now.ToUnixTimeSeconds()}>"),
                            new DiscordMessageEmbedField("Price:", item.Price),
                            new DiscordMessageEmbedField("Booth ID:", item.Id),
                            new DiscordMessageEmbedField("Translated Title: ", TranslateText(item.Title))
                        },
                        image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[0]),
                        footer: new DiscordMessageEmbedFooter(JsonConfig._config._footerText, JsonConfig._config._footerIconAvatar)));
                    for (int i = 1; i < 4 && i < item.thumbnailImageUrls.Count; i++)
                        embeds.Add(new DiscordMessageEmbed(url: $"https://booth.pm/en/items/{item.Id}", image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[i])));
                }
                DiscordMessage? message = new(username: JsonConfig._config._username, avatarUrl: JsonConfig._config._avatarUrl, tts: JsonConfig._config._tts, embeds: embeds.ToArray());

                _clients.ForEach(client =>
                {
                    StartupCheck();
                    CheckWatchList(item);
                    Thread.Sleep(1000);
                    client.SendToDiscord(message);
                });
                if (!containsBlacklisted)
                    Console.WriteLine($"{item.Title} Has been Sent!");
            }
        }



        private static async void BoothWatcher_Elapsed(object? sender = null, System.Timers.ElapsedEventArgs? e = null)
        {
            try
            {
                int newitemscount = 0;
                List<BoothItem>? boothitems = await _watcher.GetNewBoothItemAsync();
                foreach (var item in boothitems)
                {
                    if (!_alreadyAddedId.Contains(item.Id))
                    {
                        File.AppendAllText(JsonConfig._config._alreadyadded, item.Id + Environment.NewLine);
                        _alreadyAddedId.Add(item.Id);
                        _items.Enqueue(item);
                        bool containsBlacklisted =
                            (JsonConfig._config._keywordblacklist.Any(item.Title.ToLower().Contains) ||
                             JsonConfig._config._blacklist.Contains(item.ShopUrl));
                        if (!containsBlacklisted)
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
                    DiscordMessage? init = new(JsonConfig._config._startupMessage,
                        username: JsonConfig._config._username, avatarUrl: JsonConfig._config._avatarUrl,
                        tts: JsonConfig._config._tts);
                    client.SendToDiscord(init);
                    Thread.Sleep(4000);
                });
            }
        }

        private static string TranslateText(string input)
        {
            translate = !string.IsNullOrWhiteSpace(JsonConfig._config._proxyHost) ? new DeeplTranslator(selectedLanguage: Language.JP, targetLanguage: Language.EN, input, Proxyhandler.Randomprox(), new NetworkCredential(JsonConfig._config._proxyUsername, JsonConfig._config._proxyPassword)) : new DeeplTranslator(selectedLanguage: Language.JP, targetLanguage: Language.EN, input);
            return string.IsNullOrEmpty(translate.Resp) ? $"{input} \nThis Failed To translate" : translate.Resp;
        }

        private static bool IsEmpty<T>(List<T> list)
        {
            if (list == null)
            {
                return true;
            }

            return !list.Any();
        }

        private static void CheckWatchList(BoothItem item)
        {
            if (JsonConfig._config._watchlist.Contains(item.ShopUrl))
            {
                _clients.ForEach(client =>
                { 
                    DiscordMessage? watchlistitem = new($":arrow_down:  Post by author is on watchlist  :arrow_down: // <{item.ShopUrl}>", username: JsonConfig._config._username, avatarUrl: JsonConfig._config._avatarUrl, tts: JsonConfig._config._tts);
                    client.SendToDiscord(watchlistitem);
                });
            }
        }
    }
}