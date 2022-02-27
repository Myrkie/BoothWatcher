using System.Net;
using System.Text;
using System.Timers;
using ComfyUtils;
using ComfyUtils.Discord;

#pragma warning disable CS8618
#pragma warning disable CS8622
#pragma warning disable CS8625
#pragma warning disable IDE0036
#pragma warning disable IDE0044
#pragma warning disable IDE0063
#pragma warning disable IDE0090
#pragma warning disable SYSLIB0014

namespace BoothWatcher
{
    public class Program
    {
        private static bool Watching => BoothTimer.Enabled && HookTimer.Enabled;
        private static Queue<BoothItem> Items = new Queue<BoothItem>();
        private static List<string> Blacklist = new List<string>();
        private static List<string> Watchlist = new List<string>();
        private static List<Webhook> Hooks = new List<Webhook>();
        private static List<string> IDs = new List<string>();
        private static System.Timers.Timer BoothTimer;
        private static System.Timers.Timer HookTimer;
        public static Config Config => Helper.Config;
        private static ConfigHelper<Config> Helper;
        private static Booth Watcher = new Booth();
        private static string IconBase64;
        public static void Main()
        {
            Helper = new ConfigHelper<Config>($"{Environment.CurrentDirectory}\\BoothWatcherConfig.json", true);
            if (Config.Webhooks.Length > 0)
            {
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
            }
            if (Config.Watchlist.Length > 0)
            {
                Watchlist = Config.Watchlist.ToList();
            }
            if (Config.Blacklist.Length > 0)
            {
                Blacklist = Config.Blacklist.ToList();
            }
            if (File.Exists("IDs.txt"))
            {
                IDs = File.ReadAllLines("IDs.txt").ToList();
            }

            BoothTimer = new System.Timers.Timer(60 * 1000);
            HookTimer = new System.Timers.Timer(5000);
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
            Log.Msg("[4] Add User To Blacklist");
            Log.Msg($"[5] Adult Filter [{(Config.AdultFilter ? "On" : "Off")}]");
            Log.Msg($"[6] Watchlist TTS [{(Config.TTS ? "On" : "Off")}]");
            switch (Log.KeyInput().Key)
            {
                case ConsoleKey.D1:
                    if (Hooks.Count <= 0)
                    {
                        Log.Msg("No Webhooks");
                        Thread.Sleep(2500);
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
                        string[] webhooks = builder.ToString().Split('\n');
                        webhooks[webhooks.Length] = null;
                        Config.Webhooks = webhooks;
                    }
                    catch
                    {
                        Log.Msg("[Failed To Add Webhook]");
                        break;
                    }
                    Log.Msg("[Webhook Added]");
                    Thread.Sleep(2500);
                    break;

                case ConsoleKey.D3:
                    Watchlist.Add(Log.Input("Booth Author"));
                    Config.Watchlist = Watchlist.ToArray();
                    Log.Msg("[Author Added To Watchlist]");
                    Thread.Sleep(2500);
                    break;

                case ConsoleKey.D4:
                    Blacklist.Add(Log.Input("Booth Author"));
                    Config.Blacklist = Blacklist.ToArray();
                    Log.Msg("[Author Added To Blacklist]");
                    Thread.Sleep(2500);
                    break;

                case ConsoleKey.D5:
                    Config.AdultFilter = !Config.AdultFilter;
                    break;

                case ConsoleKey.D6:
                    Config.TTS = !Config.TTS;
                    break;

                default:
                    Log.KeyInput("[Invalid Key]");
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
                if (Blacklist.Contains(item.ShopURL))
                {
                    return;
                }
                List<Embed> embeds = new List<Embed>();
                Embed embed = new Embed(item.Title);
                embed.SetAuthor(item.ShopName, item.ShopURL, item.ShopImageURL);
                embed.SetURL($"https://booth.pm/en/items/{item.ID}");
                embed.AddField("Price", item.Price);
                embed.AddField("Booth ID", item.ID);
                embed.SetFooter(Config.FooterText, Config.FooterIcon);
                embeds.Add(embed);
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
            try
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
            catch
            {

            }
        }
        private static void StartupCheck()
        {
            if (string.IsNullOrEmpty(IconBase64))
            {
                using (WebClient client = new WebClient())
                {
                    IconBase64 = Convert.ToBase64String(client.DownloadData(Config.WebhookIcon));
                }
            }
            foreach (Webhook hook in Hooks)
            {
                if (hook.Name != Config.WebhookName)
                {
                    hook.Name = Config.WebhookName;
                }
                if (hook.AvatarBase64 != IconBase64)
                {
                    hook.AvatarBase64 = IconBase64;
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