using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Collections.Generic;
using Thread = System.Threading.Thread;
using ComfyUtils;
using ComfyUtils.Discord;

#pragma warning disable IDE0036
#pragma warning disable IDE0044
#pragma warning disable IDE0090

namespace BoothWatcher
{
    public class Program
    {
        private static bool Watching => BoothTimer.Enabled && HookTimer.Enabled;
        private static Queue<BoothItem> Items = new Queue<BoothItem>();
        private static List<string> Watchlist = new List<string>();
        private static List<Webhook> Hooks = new List<Webhook>();
        private static List<string> IDs = new List<string>();
        public static Config Config => Helper.Config;
        private static Booth Watcher = new Booth();
        private static ConfigHelper<Config> Helper;
        private static Timer BoothTimer;
        private static Timer HookTimer;
        public static void Main()
        {
            Helper = new ConfigHelper<Config>($"{Environment.CurrentDirectory}\\BoothWatcherConfig.json", true);
            Watchlist = Config.Watchlist.ToList();
            foreach (string webhook in Config.Webhooks)
            {
                try
                {
                    Hooks.Add(new Webhook(webhook));
                }
                catch
                {
                    Log.Msg($"[Failed To Add Webhook] {webhook}");
                }
            }
            IDs = File.ReadAllLines("IDs.txt").ToList();

            BoothTimer = new Timer(60 * 1000);
            HookTimer = new Timer(5000);
            BoothTimer.Elapsed += GetItems;
            HookTimer.Elapsed += SendItems;

            MainMenu();
            Log.Pause();
        }
        private static void MainMenu()
        {
            Log.Msg($"[1] {(Watching ? "Stop Watching" : "Start Watching")}");
            Log.Msg("[2] Add Webhook");
            Log.Msg("[3] Add User To Watchlist");
            Log.Msg($"[4] Adult Filter [{(Config.AdultFilter ? "On" : "Off")}]");
            Log.Msg($"[5] Watchlist TTS [{(Config.TTS ? "On" : "Off")}]");
            switch (Log.KeyInput().Key)
            {
                case ConsoleKey.D1:
                    if (Hooks.Count <= 0)
                    {
                        Log.Msg("No Webhooks");
                        break;
                    }
                    if (Watching)
                    {
                        BoothTimer.Stop();
                        HookTimer.Stop();
                    }
                    else
                    {
                        BoothTimer.Start();
                        HookTimer.Start();
                    }
                    break;

                case ConsoleKey.D2:
                    try
                    {
                        Hooks.Add(new Webhook(Log.Input("\nWebhook URL")));
                        StringBuilder builder = new StringBuilder();
                        foreach (Webhook hook in Hooks)
                        {
                            builder.AppendLine(hook.URL);
                        }
                        Config.Webhooks = builder.ToString().Split('\n');
                    }
                    catch
                    {
                        Log.Msg("Failed To Get Webhook");
                        break;
                    }
                    Log.Msg("Webhook Added");
                    break;

                case ConsoleKey.D3:
                    Watchlist.Add(Log.Input("Booth Author"));
                    Config.Watchlist = Watchlist.ToArray();
                    Log.Msg("Author Added");
                    break;

                case ConsoleKey.D4:
                    Config.AdultFilter = !Config.AdultFilter;
                    break;

                case ConsoleKey.D5:
                    Config.TTS = !Config.TTS;
                    break;

                default:
                    Log.KeyInput("Invalid Key");
                    break;
            }
            Log.Clear();
            MainMenu();
        }
        private static void SendItems(object sender = null, ElapsedEventArgs args = null)
        {
            if (Items.Count > 0)
            {
                StartupCheck();
                BoothItem item = Items.Dequeue();
                List<Embed> embeds = new List<Embed>();
                Embed embed = new Embed(item.Title);
                embed.SetAuthor(item.ShopName, item.ShopURL, item.ShopImageURL);
                embed.SetURL($"https://booth.pm/en/items/{item.ID}");
                embed.AddField("Price", item.Price);
                embed.AddField("Booth ID", item.ID);
                embed.SetFooter(Config.FooterText, Config.FooterIcon);
                if (item.ThumbnailURLs.Count > 0)
                {
                    embed.AddImage(item.ThumbnailURLs[0]);
                    for (int i = 1; i < 4 && i < item.ThumbnailURLs.Count; i++)
                    {
                        Embed imageEmbed = new Embed(null);
                        imageEmbed.SetURL($"https://booth.pm/en/items/{item.ID}");
                        imageEmbed.AddImage(item.ThumbnailURLs[i]);
                        embeds.Add(imageEmbed);
                    }
                }
                embeds.Add(embed);
                CheckWatchList(item);
                Thread.Sleep(1000);
                foreach (Webhook hook in Hooks)
                {
                    hook.SendEmbeds(embeds.ToArray());
                }
                Log.Msg($"[Sent] {item.Title} [{item.ID}]");
            }
        }
        private static async void GetItems(object sender = null, ElapsedEventArgs args = null)
        {
            List<BoothItem> items = await Watcher.GetNewBoothItemAsync();
            foreach (BoothItem item in items)
            {
                if (!IDs.Contains(item.ID))
                {
                    File.AppendAllText("IDs.txt", $"{item.ID}\n");
                    IDs.Add(item.ID);
                    Items.Enqueue(item);
                    Log.Msg($"[Added To Queue] {item.Title} [{item.ID}]");
                }
            }
        }
        private static void StartupCheck()
        {
            foreach (Webhook hook in Hooks)
            {
                if (hook.Name != Config.WebhookName)
                {
                    hook.Name = Config.WebhookName;
                }
            }
        }
        private static void CheckWatchList(BoothItem item)
        {
            if (Watchlist.Contains(item.ShopURL))
            {
                foreach (Webhook hook in Hooks)
                {
                    hook.SendMessage($":arrow_down: Watchlist Author Post  :arrow_down: // <{item.ShopURL}>", Config.TTS);
                }
            }
        }
    }
}