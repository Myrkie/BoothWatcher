﻿using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BoothWatcher.JSON;
using JNogueira.Discord.Webhook.Client;
using DeeplTranslator = Deepl.Deepl;
using Language = Deepl.Deepl.Language;

namespace BoothWatcher
{
    internal class BoothWatcher
    {
        static Booth _watcher = new();
        static Queue<BoothItem> _items = new();
        static List<DiscordWebhookClient> _clients = new();
        static HashSet<string> _alreadyAddedId = new();
        private static DeeplTranslator? translate;
        private static bool _warning;
        private static bool _firstartup = true;
        private static DiscordFile? _fileuploadDiscordFile;
        private static string path;

        static void Main(string[] args)
        {
            Console.Title = $"BoothWatcher - V{typeof(BoothWatcher).Assembly.GetName().Version}";

            if (File.Exists(JsonConfig._instance._alreadyadded))
                foreach (string id in File.ReadAllLines(JsonConfig._instance._alreadyadded))
                    _alreadyAddedId.Add(id);
            
            bool isEmpty = IsEmpty(JsonConfig._instance._webhook);
            if (isEmpty)
            {
                Console.WriteLine("Paste in your webhook URLs here");

                #region URLParse

                bool doLoop = true;
                while (doLoop)
                {
                    string? userinput = Console.ReadLine();
                    if (string.IsNullOrEmpty(userinput))
                    {
                        doLoop = false;
                    }
                    else
                    {
                        JsonConfig._instance._webhook.Add(userinput);
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

            if (!string.IsNullOrWhiteSpace(JsonConfig._instance._proxyHost))
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
            Thread.Sleep(-1);
        }

        private static void Startloop()
        {
            Console.WriteLine("Starting With: " + JsonConfig._instance._webhook.Count + " Webhook Connections");
            foreach (string? webhook in JsonConfig._instance._webhook)
            {
                _clients.Add(new DiscordWebhookClient(webhook));
            }

            StartupCheck();
        }

        private static void DiscordWebhook_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_items.Count != 0)
            {
                BoothItem? item = _items.Dequeue();
                List<DiscordMessageEmbed> embeds = new();

                bool containsBlacklisted = JsonConfig._instance._keywordblacklist.Any(item.Title.ToLower().Contains) ||
                                            JsonConfig._instance._blacklist.Contains(item.ShopUrl);
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
                        footer: new DiscordMessageEmbedFooter(JsonConfig._instance._footerText, JsonConfig._instance._footerIconAvatar)));
                    for (int i = 1; i < 4 && i < item.thumbnailImageUrls.Count; i++)
                        embeds.Add(new DiscordMessageEmbed(url: $"https://booth.pm/en/items/{item.Id}", image: new DiscordMessageEmbedImage(item.thumbnailImageUrls[i])));
                }

                #region FileDL/Upload
                var boothdownloader = Directory.GetFiles(Directory.GetCurrentDirectory(),"BoothDownloader" + ".*");
                if (boothdownloader.Length > 0)
                {
                    if (!containsBlacklisted && item.Price == "0 JPY")
                    {
                        var pathRegex = new Regex(@"(?<=ENVFilePATH: ).*$");

                        var proc = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "BoothDownloader",
                                Arguments = $"--booth {item.Id}",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            }
                        };

                        proc.Start();
                        while (!proc.StandardOutput.EndOfStream)
                        {
                            string? line = proc.StandardOutput.ReadLine();
                            Console.WriteLine("DownloaderSTDOut: " + line);
                            var checkMatch = pathRegex.Match(line!);
                            if (!checkMatch.Success) continue;

                            path = checkMatch.Value;
                            FileInfo fileInfo = new FileInfo(path);
                            if (fileInfo.Length < 7800000)
                            {
                                var file = File.ReadAllBytes(path);
                                var name = path.Split('/').Last();
                            
                                _fileuploadDiscordFile = new DiscordFile(name, file);
                                Console.WriteLine($"Uploading Filesize: {ConvertBytesToMegabytes(fileInfo.Length)} MB");
                            
                            }else Console.WriteLine($"File is too big skipping upload: {ConvertBytesToMegabytes(fileInfo.Length)} MB");
                        }
                        proc.StandardOutput.Close();
                        proc.Close();
                    }
                }
                else
                {
                    if (!_warning)
                    {
                        Console.WriteLine("BoothDownloader not found, ignoring download");
                    }

                    _warning = true;
                }

                #endregion
                

                DiscordMessage? message = new(username: JsonConfig._instance._username, avatarUrl: JsonConfig._instance._avatarUrl, tts: JsonConfig._instance._tts, embeds: embeds.ToArray());

                _clients.ForEach(client =>
                {
                    CheckWatchList(client, item);
                    Thread.Sleep(1000);
                    if (_fileuploadDiscordFile != null)
                    {
                        client.SendToDiscord(message, new[]{_fileuploadDiscordFile});
                        if (!JsonConfig._instance._savefiles)
                        {
                            File.Delete(path);
                            Console.WriteLine("file pushed to discord and deleted from local storage");
                        }
                        _fileuploadDiscordFile = null;
                    }else client.SendToDiscord(message);
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
                        File.AppendAllText(JsonConfig._instance._alreadyadded, item.Id + Environment.NewLine);
                        _alreadyAddedId.Add(item.Id);
                        _items.Enqueue(item);
                        bool containsBlacklisted =
                            (JsonConfig._instance._keywordblacklist.Any(item.Title.ToLower().Contains) ||
                             JsonConfig._instance._blacklist.Contains(item.ShopUrl));
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
                    Console.WriteLine("No items added to queue");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"exception at method BoothWatcher_Elapsed {exception}");
            }
        }

        private static void StartupCheck()
        {
            if (!_firstartup) return;
            {
                _firstartup = false;
                _clients.ForEach(client =>
                {
                    DiscordMessage? init = new(JsonConfig._instance._startupMessage,
                        username: JsonConfig._instance._username, avatarUrl: JsonConfig._instance._avatarUrl,
                        tts: JsonConfig._instance._tts);
                    client.SendToDiscord(init);
                    Thread.Sleep(4000);
                });
            }
        }

        private static string TranslateText(string input)
        {
            translate = !string.IsNullOrWhiteSpace(JsonConfig._instance._proxyHost) ? new DeeplTranslator(selectedLanguage: Language.JP, targetLanguage: Language.EN, input, Proxyhandler.Randomprox(), new NetworkCredential(JsonConfig._instance._proxyUsername, JsonConfig._instance._proxyPassword)) : new DeeplTranslator(selectedLanguage: Language.JP, targetLanguage: Language.EN, input);
            return string.IsNullOrEmpty(translate.Resp) ? $"{input} \nThis Failed To translate" : translate.Resp;
        }
        
        // convert byte to megabyte and round to 2 decimal places
        private static double ConvertBytesToMegabytes(long bytes)
        {
            return Math.Round((bytes / 1024f) / 1024f, 2);
        }
        
        private static bool IsEmpty<T>(List<T> list)
        {
            if (list == null)
            {
                return true;
            }

            return !list.Any();
        }

        private static void CheckWatchList(DiscordWebhookClient client, BoothItem item)
        {
            Console.WriteLine("watchlist checked");
            if (JsonConfig._instance._watchlist.Contains(item.ShopUrl))
            {
                DiscordMessage? watchlistitem = new($"{JsonConfig._instance._notificationtext} <{item.ShopUrl}>", username: JsonConfig._instance._username, avatarUrl: JsonConfig._instance._avatarUrl, tts: JsonConfig._instance._tts);
                client.SendToDiscord(watchlistitem);
            }
        }
    }
}