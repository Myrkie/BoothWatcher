using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoothWatcher.JSON
{
    [JsonSerializable(typeof(JsonConfig))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = true, AllowTrailingCommas = true)]
    internal partial class ConfigSourceGenerationContext : JsonSerializerContext;

    [Serializable]
    public class JsonConfig
    {
        private static FileSystemWatcher _fileSystemWatcher = new();
        static JsonConfig()
        {
            
            _instance = LoadConfig();
            
            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Path = Directory.GetCurrentDirectory();
            _fileSystemWatcher.Filter = ConfigPath;
            _fileSystemWatcher.Changed += (_, args) =>
            {
                if (args.ChangeType != WatcherChangeTypes.Changed) return;

                Console.WriteLine("Config file changed, reloading");
                var reloaded = LoadConfig();
                reloaded.SaveConfig(); 
                Thread.Sleep(5000);
            };
            
            _fileSystemWatcher.EnableRaisingEvents = true;
        }
        
        public static readonly string ConfigPath = "BWConfig.json";
        public static JsonConfig _instance { get; set; }
        
        [JsonPropertyName("Embed FooterText")] 
        public string _footerText { get; set; } = "Made by Keafy & Myrkur";
        
        [JsonPropertyName("Startup Message")] 
        public string _startupMessage { get; set; } = "Starting Up";

        [JsonPropertyName("WebHook Override name")]
        public string _username { get; set; } = $"BoothWatcher - V{typeof(BoothWatcher).Assembly.GetName().Version}";

        [JsonPropertyName("Avatar Icon")] 
        public string _avatarUrl { get; set; } = "https://i.imgur.com/gEJk8uX.jpg";

        [JsonPropertyName("Embed Footer Icon")]
        public string _footerIconAvatar { get; set; } = "https://i.imgur.com/gEJk8uX.jpg";

        [JsonPropertyName("Webhooks")] 
        public List<string?> _webhook { get; set; } = new();
        [JsonPropertyName("Watchlist")] 
        public List<string> _watchlist { get; set; } = new();
        [JsonPropertyName("Blacklist")] 
        public List<string> _blacklist { get; set; } = new();
        [JsonPropertyName("KeywordBlacklist")] 
        public List<string> _keywordblacklist { get; set; } = new();
        [JsonPropertyName("Already Posted list")] 
        public string _alreadyadded { get; set; } = "AlreadyAddedId.txt";
        [JsonPropertyName("Proxy Username")] 
        public string _proxyUsername { get; set; } = "";
        [JsonPropertyName("Proxy Password")] 
        public string _proxyPassword { get; set; } = "";
        [JsonPropertyName("Proxy Host")] 
        public string _proxyHost { get; set; } = "";

        public bool _tts { get; set; }
        public bool _savefiles { get; set; }

        [JsonPropertyName("WatchListNotification")]
        public string _notificationtext { get; set; } = "// :arrow_down:  Post by author is on watchlist  :arrow_down: //";

        static JsonConfig LoadConfig()
        {
            JsonConfig? cfg = File.Exists(ConfigPath) ? JsonSerializer.Deserialize(File.ReadAllText(ConfigPath), ConfigSourceGenerationContext.Default.JsonConfig) : null;
            if(cfg == null)
            {
                cfg = new JsonConfig();
                cfg.SaveConfig();
            }

            _instance = cfg;

            return cfg;
        }

        void SaveConfig()
        {
            string json = JsonSerializer.Serialize(_instance, ConfigSourceGenerationContext.Default.JsonConfig);
            File.WriteAllText(ConfigPath, json);
        }
    }
}