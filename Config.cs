#pragma warning disable CA1825

namespace BoothWatcher
{
    public class Config
    {
        public string StartupMessage { get; set; } = "Starting Up...";
        public bool AdultFilter { get; set; } = false;
        public string WebhookName { get; set; } = $"BoothWatcher - v{typeof(Program).Assembly.GetName().Version}";
        public string WebhookIcon { get; set; } = "https://booth.pm/static-images/pwa/icon_size_180.png";
        public string[] Webhooks { get; set; } = new string[0];
        public string[] Watchlist { get; set; } = new string[0];
        public string[] Blacklist { get; set; } = new string[0];
        public string FooterText { get; set; } = "Made by Keafy & Myrkur, branch edit by Boppr";
        public string FooterIcon { get; set; } = "https://cdn.discordapp.com/avatars/762362283195498546/2bfe26489837b80916b1ec2ab7144fa5.webp";
        public bool TTS { get; set; } = false;
    }
}
