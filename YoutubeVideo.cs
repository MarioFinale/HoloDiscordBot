using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloDiscordBot
{
    public class YoutubeVideo
    {
        public string Name { get; }
        public string Url { get; }
        public DateTime Publish { get; }
        public bool Live { get; }
        public YoutubeVideo(string url, DateTime publish, bool live, string name)
        {
            Name = name;
            Url = url;
            Publish = publish;
            Live = live;
        }
    }
}
