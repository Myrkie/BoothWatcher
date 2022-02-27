using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

#pragma warning disable IDE0044
#pragma warning disable IDE0090
#pragma warning disable IDE1006

namespace ComfyUtils.Discord
{
    public class Webhook
    {
        public class Message
        {
            public string id { get; set; }
            public int type { get; set; }
            public string content { get; set; }
            public string channel_id { get; set; }
            public Author author { get; set; }
            public object[] attachments { get; set; }
            public object[] embeds { get; set; }
            public object[] mentions { get; set; }
            public object[] mention_roles { get; set; }
            public bool pinned { get; set; }
            public bool mention_everyone { get; set; }
            public bool tts { get; set; }
            public string timestamp { get; set; }
            public string edited_timestamp { get; set; }
            public int flags { get; set; }
            public object[] components { get; set; }
            public string webhook_id { get; set; }
            public class Author
            {
                public bool bot { get; set; }
                public string id { get; set; }
                public string username { get; set; }
                public string avatar { get; set; }
                public string discriminator { get; set; }
            }
        }
        internal class InternalWebhook
        {
            public string id { get; set; }
            public string guild_id { get; set; }
            public string channel_id { get; set; }
            public string name { get; set; }
            public string avatar { get; set; }
            public string token { get; set; }
        }
        internal readonly string BaseURL = "https://discord.com/api/webhooks";
        public string URL { get; private set; }
        public string ID { get; }
        public string Token { get; }
        internal string InternalName;
        public string Name
        {
            get => InternalName;
            set
            {
                InternalName = value;
                HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}");
                request.Method = "PATCH";
                request.ContentType = "application/json";
                Stream requestStream = request.GetRequestStream();
                byte[] data = Encoding.UTF8.GetBytes($"{{\"name\":\"{value}\"}}");
                requestStream.Write(data, 0, data.Length);
                request.GetResponse();
            }
        }
        internal byte[] InternalAvatar;
        public byte[] Avatar
        {
            get => InternalAvatar;
            set
            {
                InternalAvatar = value;
                HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}");
                request.Method = "PATCH";
                request.ContentType = "application/json";
                Stream requestStream = request.GetRequestStream();
                byte[] data = Encoding.UTF8.GetBytes($"{{\"avatar\":\"data:image/jpeg;base64, {Convert.ToBase64String(value)}\"}}");
                requestStream.Write(data, 0, data.Length);
                request.GetResponse();
            }
        }
        public string ServerID { get; }
        public string ChannelID { get; }
        public Webhook(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            char[] read = new char[256];
            int count = responseStream.Read(read, 0, 256);
            StringBuilder builder = new StringBuilder();
            while (count > 0)
            {
                string str = new string(read, 0, count);
                builder.Append(str);
                count = responseStream.Read(read, 0, 256);
            }
            InternalWebhook hook = JsonConvert.DeserializeObject<InternalWebhook>(builder.ToString());
            URL = url;
            ID = hook.id;
            Token = hook.token;
            InternalName = hook.name;
            InternalAvatar = hook.avatar != null ? Convert.FromBase64String(hook.avatar) : null;
            ServerID = hook.guild_id;
            ChannelID = hook.channel_id;
        }
        public Message GetMessage(string messageID)
        {
            HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}/messages/{messageID}");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            char[] read = new char[256];
            int count = responseStream.Read(read, 0, 256);
            StringBuilder builder = new StringBuilder();
            while (count > 0)
            {
                string str = new string(read, 0, count);
                builder.Append(str);
                count = responseStream.Read(read, 0, 256);
            }
            return JsonConvert.DeserializeObject<Message>(builder.ToString());
        }
        public Message SendMessage(string content, bool tts = false)
        {
            HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}?wait=true");
            request.Method = "POST";
            request.ContentType = "application/json";
            Stream requestStream = request.GetRequestStream();
            byte[] data = Encoding.UTF8.GetBytes($"{{\"content\":\"{content}\",\"tts\":{tts}}}");
            requestStream.Write(data, 0, data.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            char[] read = new char[256];
            int count = responseStream.Read(read, 0, 256);
            StringBuilder builder = new StringBuilder();
            while (count > 0)
            {
                string str = new string(read, 0, count);
                builder.Append(str);
                count = responseStream.Read(read, 0, 256);
            }
            return JsonConvert.DeserializeObject<Message>(builder.ToString());
        }
        public Message SendEmbed(Embed embed)
        {
            embed.type = "rich";
            HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}?wait=true");
            request.Method = "POST";
            request.ContentType = "application/json";
            Stream requestStream = request.GetRequestStream();
            byte[] data = Encoding.UTF8.GetBytes($"{{\"embeds\":[{JsonConvert.SerializeObject(embed)}]}}");
            requestStream.Write(data, 0, data.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            char[] read = new char[256];
            int count = responseStream.Read(read, 0, 256);
            StringBuilder builder = new StringBuilder();
            while (count > 0)
            {
                string str = new string(read, 0, count);
                builder.Append(str);
                count = responseStream.Read(read, 0, 256);
            }
            return JsonConvert.DeserializeObject<Message>(builder.ToString());
        }
        public Message SendEmbeds(Embed[] embeds)
        {
            foreach (Embed embed in embeds)
            {
                embed.type = "rich";
            }
            HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}?wait=true");
            request.Method = "POST";
            request.ContentType = "application/json";
            Stream requestStream = request.GetRequestStream();
            byte[] data = Encoding.UTF8.GetBytes($"{{\"embeds\":[{JsonConvert.SerializeObject(embeds)}]}}");
            requestStream.Write(data, 0, data.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            char[] read = new char[256];
            int count = responseStream.Read(read, 0, 256);
            StringBuilder builder = new StringBuilder();
            while (count > 0)
            {
                string str = new string(read, 0, count);
                builder.Append(str);
                count = responseStream.Read(read, 0, 256);
            }
            return JsonConvert.DeserializeObject<Message>(builder.ToString());
        }
        public Message SendEmbed(string embed)
        {
            HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}?wait=true");
            request.Method = "POST";
            request.ContentType = "application/json";
            Stream requestStream = request.GetRequestStream();
            byte[] data = Encoding.UTF8.GetBytes($"{{\"embeds\":[{embed}]}}");
            requestStream.Write(data, 0, data.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            char[] read = new char[256];
            int count = responseStream.Read(read, 0, 256);
            StringBuilder builder = new StringBuilder();
            while (count > 0)
            {
                string str = new string(read, 0, count);
                builder.Append(str);
                count = responseStream.Read(read, 0, 256);
            }
            return JsonConvert.DeserializeObject<Message>(builder.ToString());
        }
        public Message EditMessage(string messageID, string content)
        {
            HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}/messages/{messageID}?wait=true");
            request.Method = "PATCH";
            request.ContentType = "application/json";
            Stream requestStream = request.GetRequestStream();
            byte[] data = Encoding.UTF8.GetBytes($"{{\"content\":\"{content}\"}}");
            requestStream.Write(data, 0, data.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            char[] read = new char[256];
            int count = responseStream.Read(read, 0, 256);
            StringBuilder builder = new StringBuilder();
            while (count > 0)
            {
                string str = new string(read, 0, count);
                builder.Append(str);
                count = responseStream.Read(read, 0, 256);
            }
            return JsonConvert.DeserializeObject<Message>(builder.ToString());
        }
        public bool DeleteMessage(string messageID)
        {
            HttpWebRequest request = WebRequest.CreateHttp($"{BaseURL}/{ID}/{Token}/messages/{messageID}");
            request.Method = "DELETE";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.StatusCode == HttpStatusCode.NoContent;
        }
    }
    [Serializable]
    public class Embed
    {
        [Serializable]
        public class Footer
        {
            public string text { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
        }
        [Serializable]
        public class Media
        {
            public string url { get; set; }
            public string proxy_url { get; set; }
            public int height { get; set; }
            public int width { get; set; }
        }
        [Serializable]
        public class Provider
        {
            public string name { get; set; }
            public string url { get; set; }
        }
        [Serializable]
        public class Author
        {
            public string name { get; set; }
            public string url { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
        }
        [Serializable]
        public class Field
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
        }
        public string title { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public DateTime timestamp { get; set; }
        public int color { get; set; }
        public Footer footer { get; set; }
        public Media image { get; set; }
        public Media thumbnail { get; set; }
        public Media video { get; set; }
        public Provider provider { get; set; }
        public Author author { get; set; }
        public Field[] fields { get; set; }
        private List<Field> Fields;
        public Embed(string title, string description = null)
        {
            this.title = title;
            this.description = description;
            Fields = new List<Field>(25);
        }
        public Field AddField(string name, string value, bool inline = false)
        {
            Field field = new Field
            {
                name = name,
                value = value,
                inline = inline
            };
            Fields.Add(field);
            fields = Fields.ToArray();
            return field;
        }
        public Footer SetFooter(string text, string icon = null)
        {
            footer.text = text;
            footer.icon_url = icon ?? null;
            return footer;
        }
        public void SetURL(string url) => this.url = url;
        public Author SetAuthor(string name, string url = null, string icon = null)
        {
            Author author = new Author()
            {
                name = name,
                url = url ?? null,
                icon_url = icon ?? null
            };
            this.author = author;
            return author;
        }
        public Media AddImage(string url)
        {
            Media image = new Media
            {
                url = url,
                width = 1200,
                height = 900
            };
            this.image = image;
            return image;
        }
    }
}
