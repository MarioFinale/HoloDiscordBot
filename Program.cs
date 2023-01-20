using Discord.WebSocket;
using Discord;
using System;
using System.Globalization;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Net.WebRequestMethods;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Channels;

namespace HoloDiscordBot
{
    public class Program
    {
        DiscordSocketClient Client;

        public static Task Main(string[] args) => new Program().MainAsync();


        public async Task MainAsync()
        {
            if (!System.IO.File.Exists("./channels.txt")) return;
            if (!System.IO.File.Exists("./token.txt")) return;
            if (!System.IO.File.Exists("./list.txt")) return;
            Console.WriteLine("----------------------------------- ooooo -----------------------------------");
            Console.Write("  _    _       _       _____  _                       _ ____        _   \r\n | |  | |     | |     |  __ \\(_)                     | |  _ \\      | |  \r\n | |__| | ___ | | ___ | |  | |_ ___  ___ ___  _ __ __| | |_) | ___ | |_ \r\n |  __  |/ _ \\| |/ _ \\| |  | | / __|/ __/ _ \\| '__/ _` |  _ < / _ \\| __|\r\n | |  | | (_) | | (_) | |__| | \\__ \\ (_| (_) | | | (_| | |_) | (_) | |_ \r\n |_|  |_|\\___/|_|\\___/|_____/|_|___/\\___\\___/|_|  \\__,_|____/ \\___/ \\__|\r\n                                                                        \r\n                                                                        \r\n\r\n");
            Console.WriteLine("----------------------------------- ooooo -----------------------------------");
            Console.WriteLine("Ver. 0.0.0.0.0.0.0.0.0.0.0.0.0.0.1 IDK IDC");
            Console.WriteLine("----------------------------------- ooooo -----------------------------------");
            Console.WriteLine();
            Console.WriteLine();
            ulong[] channels = Array.ConvertAll(System.IO.File.ReadAllLines("./channels.txt"), ulong.Parse);

            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.Guilds,

            };

            Client = new DiscordSocketClient(config);

            Client.Log += Utils.Log;
            string token = System.IO.File.ReadAllText("./token.txt");
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            Client.Ready += () =>
            {
                Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "Updater", "Bot is connected!"));

                while (true)
                {
                    Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "Updater", "Updating info!"));
                    YoutubeChannel[] ytChannels = GetChannels().ToArray();
                    string nextStreamsAndLives = GetNextStreamsAndLives(ytChannels);
                    string nextShort = GetNextStreamsShort(ytChannels);

                    foreach (ulong channel in channels)
                    {
                        var Channel = Client.GetChannel(channel) as SocketTextChannel;
                        IEnumerable<IMessage> messages = Channel.GetMessagesAsync().Flatten().ToEnumerable();
                        Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "Updater", "Deleting channel messages..."));
                        Channel.DeleteMessagesAsync(messages);
                        Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "Updater", "Updating next Streams message..."));
                        Channel.SendMessageAsync(nextStreamsAndLives);
                        Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "Updater", "Updating channel name..."));
                        Channel.ModifyAsync(prop => prop.Name = nextShort);
                        Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "Updater", "Done!"));
                    }
                    Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "Updater", "Sleeping for 5 minutes..."));
                    System.Threading.Thread.Sleep(300000); //5min delay
                }
            };
            // Block this task until the program is closed.
            await Task.Delay(-1);

        }


        public static List<YoutubeChannel> GetChannels()
        {
            string[] channels = System.IO.File.ReadAllLines("./list.txt");
            List<YoutubeChannel> YTchannels = new List<YoutubeChannel>();
            foreach (string line in channels)
            {
                string name = line.Split('|')[0];
                string emoji = line.Split('|')[1];
                YoutubeChannel channel = new YoutubeChannel(name, emoji);
                YTchannels.Add(channel);
            }
            return YTchannels;
        }

        public static string GetNextStreamsShort(YoutubeChannel[] channels)
        {
            DateTime now = DateTime.Now;

            List<YoutubeChannel> LiveChannels = new List<YoutubeChannel>();
            List<YoutubeChannel> UpcomingChannels = new List<YoutubeChannel>();

            foreach (YoutubeChannel channel in channels)
            {
                if (channel.Live) LiveChannels.Add(channel);
            }

            foreach (YoutubeChannel channel in channels)
            {
                if (!LiveChannels.Contains(channel))
                {
                    if (DateTime.Compare(now, channel.LatestLiveOrNext) < 0)
                    {
                        UpcomingChannels.Add(channel);
                    }
                }
            }

            string textToPrint = string.Empty;

            if (LiveChannels.Count > 0)
            {
                string text = "Live ";
                foreach (YoutubeChannel channel in LiveChannels)
                {
                    text += channel.Emoji + " ";

                }
                text = text.Trim();
                text = text.Replace(' ', '-');
                textToPrint += text;

            }
            else
            {
                if (UpcomingChannels.Count > 0)
                {
                    UpcomingChannels = UpcomingChannels.OrderBy(a => a.LatestLiveOrNext).ToList();
                    string text = "Next " + UpcomingChannels[0].Emoji;
                    if (UpcomingChannels.Count > 1) text += UpcomingChannels[1].Emoji;
                    text = text.Trim();
                    textToPrint += text;
                }
                else
                {
                    textToPrint += "No streams";
                }
            }
            return textToPrint.Replace(' ', '_');
        }

        public static string GetNextStreamsAndLives(YoutubeChannel[] channels)
        {
            DateTime now = DateTime.Now;

            List<YoutubeChannel> LiveChannels = new List<YoutubeChannel>();
            List<YoutubeChannel> UpcomingChannels = new List<YoutubeChannel>();

            foreach (YoutubeChannel channel in channels)
            {
                if (channel.Live) LiveChannels.Add(channel);
            }

            foreach (YoutubeChannel channel in channels)
            {
                if (!LiveChannels.Contains(channel))
                {
                    if (DateTime.Compare(now, channel.LatestLiveOrNext) < 0)
                    {
                        UpcomingChannels.Add(channel);
                    }
                }
            }

            string textToPrint = string.Empty;

            if (LiveChannels.Count > 0)
            {
                string text = "Live now: ";
                foreach (YoutubeChannel channel in LiveChannels)
                {
                    text += channel.Emoji + "-";

                }
                text = text.Trim('-');
                foreach (YoutubeChannel channel in LiveChannels)
                {
                    text += "\n - " + channel.Name + " " + channel.Emoji;
                }
                textToPrint += text + "\n";

            }

            if (UpcomingChannels.Count > 0)
            {
                UpcomingChannels = UpcomingChannels.OrderBy(a => a.LatestLiveOrNext).ToList();

                string text = "Upcoming Streams: ";
                foreach (YoutubeChannel channel in UpcomingChannels)
                {
                    text += "\n - " + Utils.CapitalizeFirstLetter(channel.Name) + " " + channel.Emoji + " | On <t:" + new DateTimeOffset(channel.LatestLiveOrNext.ToUniversalTime()).ToUnixTimeSeconds() + "> | ||<" + channel.LatestLiveOrNextUrl + ">||";
                }
                textToPrint += text + "\n";
            }
            else
            {
                textToPrint += "\nNo upcoming streams!\n";
            }
            return textToPrint;
        }
    }
}
