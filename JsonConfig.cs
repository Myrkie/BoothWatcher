using Newtonsoft.Json;

namespace BoothWatcher
{
    public class JsonConfig
    {
        public static string _configpath = "BWConfig.json";
        public static Config _config = new Config();

        public class Config
        {
            [JsonProperty("Embed FooterText")]
            public string _footerText { get; set; } = "Made by Keafy & Myrkur";
            [JsonProperty("Startup Message")]
            public string _startupMessage { get; set; } = "Starting Up";
            [JsonProperty("WebHook Override name")]
            public string _username { get; set; } = $"BoothWatcher - V{typeof(BoothWatcher).Assembly.GetName().Version}";
            [JsonProperty("Avatar Icon")]
            public string _avatarUrl { get; set; } = "https://i.imgur.com/gEJk8uX.jpg";
            [JsonProperty("Embed Footer Icon")]
            public string _footerIconAvatar { get; set; } = "https://i.imgur.com/gEJk8uX.jpg";
            [JsonProperty("Webhooks")]
            public List<string> _webhook { get; set; } = new(); 
            [JsonProperty("Watchlist")]
            public List<string> _watchlist { get; set; } = new();
            [JsonProperty("Blacklist")]
            public List<string> _blacklist { get; set; } = new();
            [JsonProperty("KeywordBlacklist")]
            public List<string> _keywordblacklist { get; set; } = new();
            [JsonProperty("Already Posted list")]
            public string _alreadyadded { get; set; } = "AlreadyAddedId.txt";
            [JsonProperty("Proxy Username")]
            public string _proxyUsername { get; set; } = "";
            [JsonProperty("Proxy Password")]
            public string _proxyPassword { get; set; } = "";
            [JsonProperty("Proxy Host")]
            public string _proxyHost { get; set; } = "";
            
            public bool _tts { get; set; } = false;
        }

        public static class Configure
        {
            public static void load()
            {
                try
                {
                    if (!File.Exists(_configpath))
                    {
                        saveconf();
                    }
                    else
                    {
                        loadconf();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            public static void forcesave() => saveconf();
            private static void loadconf() => _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configpath));

            private static void saveconf() => File.WriteAllText(_configpath, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }
    }
}