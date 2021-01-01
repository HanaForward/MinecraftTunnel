using MinecraftTunnel.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace MinecraftTunnel.Protocol
{
    public class Chat : IProtocol
    {
        [JsonIgnore]
        public int PacketId => 0;
        public string text { get; set; }
        public string color { get; set; }
        public bool bold { get; set; }
        public List<Extra> extras { get; set; }
        public Chat() { }
        public Chat(Extra extra)
        {
            this.text = extra.text;
            this.color = extra.color;
            this.bold = extra.bold;
        }
        public Chat(Extra extra, List<Extra> extras) : this(extra)
        {
            this.extras = extras;
        }
        public void Analyze(Block block)
        {
            throw new System.NotImplementedException();
        }
        public byte[] Pack()
        {
            var options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                IgnoreNullValues = true,
                WriteIndented = false,
            };
            using (MemoryStream memoryStream = new MemoryStream())
            {
                string str = JsonSerializer.Serialize(this, options);
                memoryStream.WriteInt(PacketId);
                memoryStream.WriteString(str, true);
                return memoryStream.ToArray();
            }
        }
    }
    public class Extra
    {
        public string text { get; set; }
        public string color { get; set; }
        public bool bold { get; set; }
        public Extra() { }
    }
}
