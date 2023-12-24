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
using Newtonsoft.Json.Linq;

namespace HoloDiscordBot
{
    public class Program
    {
        public static Task Main()
        {
            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            // Check if necessary files exist, return if not
            if (!System.IO.File.Exists("./channels.txt")) {
                await Utils.Log(new LogMessage(LogSeverity.Error, "MainAsync", "channels.txt is missing!"));
                return;
            } 
            if (!System.IO.File.Exists("./token.txt"))
            {
                await Utils.Log(new LogMessage(LogSeverity.Error, "MainAsync", "token.txt is missing!"));
                return;
            }
            if (!System.IO.File.Exists("./list.txt"))
            {
                await Utils.Log(new LogMessage(LogSeverity.Error, "MainAsync", "list.txt is missing!"));
                return;
            }

            // Display the ASCII art and version information
            DisplayStartupInformation();

            // Configure the Discord client
            var discordConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.Guilds,
            };

            // Initialize the Discord client
            var discordClient = InitializeDiscordClient(discordConfig);

            // Read the bot token from the file
            string botToken = System.IO.File.ReadAllText("./token.txt");

            // Log in and start the Discord client
            await LoginAndStartClient(discordClient, botToken);

            // Wait for the client to be connected
            await WaitForClientConnection(discordClient);

            // Log information about the bot being connected
            await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", "Bot is connected!"));

            // Call the main loop
            await MainLoop(discordClient, discordConfig, botToken);
        }

        /// <summary>
        /// Displays the ASCII art and version information
        /// </summary>
        private void DisplayStartupInformation()
        {
            Console.WriteLine("----------------------------------- ooooo -----------------------------------");
            Console.Write("  _    _       _       _____  _                       _ ____        _   \r\n | |  | |     | |     |  __ \\(_)                     | |  _ \\      | |  \r\n | |__| | ___ | | ___ | |  | |_ ___  ___ ___  _ __ __| | |_) | ___ | |_ \r\n |  __  |/ _ \\| |/ _ \\| |  | | / __|/ __/ _ \\| '__/ _` |  _ < / _ \\| __|\r\n | |  | | (_) | | (_) | |__| | \\__ \\ (_| (_) | | | (_| | |_) | (_) | |_ \r\n |_|  |_|\\___/|_|\\___/|_____/|_|___/\\___\\___/|_|  \\__,_|____/ \\___/ \\__|\r\n                                                                        \r\n                                                                        \r\n\r\n");
            Console.WriteLine("----------------------------------- ooooo -----------------------------------");
            Console.WriteLine("Ver. 1.1");
            Console.WriteLine("----------------------------------- ooooo -----------------------------------");
            Console.WriteLine();
            Console.WriteLine();
        }

        /// <summary>
        /// Initializes the Discord client
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private DiscordSocketClient InitializeDiscordClient(DiscordSocketConfig config)
        {
            // Create and configure the Discord client
            var client = new DiscordSocketClient(config);

            // Set up event handler for logging
            client.Log += Utils.Log;

            return client;
        }

        /// <summary>
        /// Logs in and starts the Discord client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task LoginAndStartClient(DiscordSocketClient client, string token)
        {
            // Log in to Discord with the bot token
            await client.LoginAsync(TokenType.Bot, token);

            // Start the Discord client
            await client.StartAsync();
        }

        /// <summary>
        /// Waits for the client to be connected
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private async Task WaitForClientConnection(DiscordSocketClient client)
        {
            // Wait for the client to be connected
            while (client.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Main loop
        /// </summary>
        /// <param name="client"></param>
        /// <param name="config"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        async Task MainLoop(DiscordSocketClient client, DiscordSocketConfig config, string token)
        {
            while (true)
            {
                // Read channel IDs from the file
                ulong[] channels = Array.ConvertAll(System.IO.File.ReadAllLines("./channels.txt"), ulong.Parse);

                try
                {
                    // Log information about the update process
                    await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", "Updating info!"));

                    // Get YouTube channels from the file
                    YoutubeChannel[] ytChannels = GetChannels().ToArray();

                    // Get information about the next streams and lives
                    string nextStreamsAndLives = GetNextStreamsAndLives(ytChannels);

                    // Get short information about the next streams
                    string nextShort = GetNextStreamsShort(ytChannels);

                    try
                    {
                        // Reinitialize the Discord client if not logged in
                        if (client.LoginState != LoginState.LoggedIn)
                        {
                            client = new DiscordSocketClient(config);
                            client.Log += Utils.Log;
                            await client.LoginAsync(TokenType.Bot, token);
                            await client.StartAsync();
                            await WaitForClientConnection(client);
                            await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", "Bot is connected!"));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log any exceptions during client initialization
                        await Utils.Log(new LogMessage(LogSeverity.Info, "Err Hand 0", ex.Message));
                    }

                    // Set the bot's activity
                    await client.SetGameAsync("Pekora", null, ActivityType.Watching);

                    // Iterate through each configured channel
                    foreach (ulong channelId in channels)
                    {
                        // Check if the channel is a text channel
                        if (client.GetChannel(channelId) is not ITextChannel channel)
                            continue;

                        // Delete all messages in the channel
                        await DeleteAllMessages(channel);

                        // Update the next streams message
                        await UpdateNextStreamsMessage(channel, nextStreamsAndLives);

                        // Modify the channel name
                        await ModifyChannelName(channel, nextShort);
                    }

                    // Log completion and sleep for 5 minutes
                    await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", "Done!"));
                    await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", "Sleeping for 5 minutes..."));
                    await Task.Delay(300000); // 5min delay
                }
                catch (Exception ex)
                {
                    // Log any exceptions during the update process and sleep for 5 minutes
                    await Utils.Log(new LogMessage(LogSeverity.Info, "Err Hand 2", ex.Message));
                    await Task.Delay(300000); // 5min delay
                }
            }
        }

        /// <summary>
        /// Deletes all messages in the supplied channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        async Task DeleteAllMessages(ITextChannel channel)
        {
            // Retrieve all messages in the channel
            IEnumerable<IMessage> messages = await channel.GetMessagesAsync().FlattenAsync();

            // Check if there are any messages to delete
            if (messages.Any())
            {
                // Log information about the deletion
                await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", $"Deleting {messages.Count()} channel messages..."));

                // Delete all messages in the channel
                await channel.DeleteMessagesAsync(messages);
            }
        }

        /// <summary>
        /// Sends an updated "Live now" and "Next streams" message to the supplied channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nextStreamsAndLives"></param>
        /// <returns></returns>
        async Task UpdateNextStreamsMessage(ITextChannel channel, string nextStreamsAndLives)
        {
            // Check if the message length is over 1999 characters
            if (nextStreamsAndLives.Length > 1999)
            {
                // Log information about splitting the message
                await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", "Message length is over 2000 characters, splitting..."));

                // Split the message into parts
                string[] messages = SplitMessage(nextStreamsAndLives);

                // Send the first part of the message
                await channel.SendMessageAsync(messages[0]);

                // Check if the second part is still too long
                if (messages[1].Length > 1999)
                {
                    // Send the second part in smaller chunks
                    await SendMessageInParts(channel, messages[1]);
                }
                else
                {
                    // Send the second part as a whole
                    await channel.SendMessageAsync(messages[1]);
                }
            }
            else
            {
                // Send the entire message
                await channel.SendMessageAsync(nextStreamsAndLives);
            }
        }

        /// <summary>
        /// Sends a message in parts;
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        async Task SendMessageInParts(ITextChannel channel, string message)
        {
            int index = 1999;

            // Iterate through the message to find a suitable split point
            while (true)
            {
                // Check for a newline followed by '-' and a space
                char c1 = message[index];
                char c2 = message[index + 1];
                char c3 = message[index + 2];

                // Check if the split point is found
                if (c1 == '\n' && c2 == '-' && c3 == ' ')
                {
                    // Send the first part of the message
                    await channel.SendMessageAsync(message[..index]);

                    // Send the remaining part of the message
                    await channel.SendMessageAsync(message[index..]);
                    break;
                }

                // Move to the previous character
                index--;

                // Check if the index goes out of bounds
                if (index < 0)
                {
                    // If no suitable split point is found, send the entire message
                    await channel.SendMessageAsync(message);
                    break;
                }
            }
        }

        /// <summary>
        /// Changes a channel name to the supplied string
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nextShort"></param>
        /// <returns></returns>
        async Task ModifyChannelName(ITextChannel channel, string nextShort)
        {
            // Log information about updating the channel name
            await Utils.Log(new LogMessage(LogSeverity.Info, "Updater", "Updating channel name..."));

            // Modify the channel's name property
            await channel.ModifyAsync(properties => properties.Name = nextShort);
        }

        /// <summary>
        /// Splits a message in the best way possible to be sent in parts.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        string[] SplitMessage(string message)
        {
            // Array to store the split messages
            string[] messages = new string[2];

            // Start index for splitting
            int index = 1999;

            // Iterate through the message to find a suitable split point
            while (true)
            {
                // Check for a newline followed by '-' and a space
                char c1 = message[index];
                char c2 = message[index + 1];
                char c3 = message[index + 2];

                // Check if the split point is found
                if (c1 == '\n' && c2 == '-' && c3 == ' ')
                {
                    // Set the first part of the message
                    messages[0] = message[..index];

                    // Set the remaining part of the message
                    messages[1] = message[index..];
                    break;
                }

                // Move to the previous character
                index--;

                // Check if the index goes out of bounds
                if (index < 0)
                {
                    // If no suitable split point is found, set the entire message
                    messages[0] = message;
                    messages[1] = string.Empty;
                    break;
                }
            }

            // Return the array of split messages
            return messages;
        }

        /// <summary>
        /// Gets a list of youtube channels from the /list.txt text file.
        /// </summary>
        /// <returns></returns>
        public static List<YoutubeChannel> GetChannels()
        {
            // Read all lines from the file containing channel information
            string[] channelLines = System.IO.File.ReadAllLines("./list.txt");

            // List to store YouTube channels
            List<YoutubeChannel> youtubeChannels = new();

            // Iterate through each line in the file
            foreach (string line in channelLines)
            {
                // Split the line using the '|' character to extract channel name and emoji
                string[] channelInfo = line.Split('|');

                // Ensure that the line has the expected format
                if (channelInfo.Length == 2)
                {
                    // Extract channel name and emoji
                    string name = channelInfo[0];
                    string emoji = channelInfo[1];

                    // Create a new YoutubeChannel object
                    YoutubeChannel channel = new(name, emoji);

                    // Add the channel to the list
                    youtubeChannels.Add(channel);
                }
                else
                {
                    // Log warning for unexpected line format
                    Utils.Log(new LogMessage(LogSeverity.Warning, "GetChannels", $"Skipping line with unexpected format: {line}"));
                }
            }

            // Return the list of YouTube channels
            return youtubeChannels;
        }


        /// <summary>
        /// Generates a short string with the emojis of the channels with livestreams or scheduled streams/videos.
        /// </summary>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static string GetNextStreamsShort(YoutubeChannel[] channels)
        {
            // Get the current date and time
            DateTime now = DateTime.Now;

            // List to store channels currently live
            List<YoutubeChannel> liveChannels = new();

            // List to store upcoming channels
            List<YoutubeChannel> upcomingChannels = new();

            // Iterate through channels to categorize them
            foreach (YoutubeChannel channel in channels)
            {
                // Check if the channel is currently live
                if (channel.Live)
                {
                    liveChannels.Add(channel);
                }
            }

            // Iterate through channels to find upcoming channels
            foreach (YoutubeChannel channel in channels)
            {
                // Check if the channel is not in the live channels list
                if (!liveChannels.Contains(channel))
                {
                    // Check if the channel's latest live or next stream is in the future
                    if (DateTime.Compare(now, channel.LatestLiveOrNext) < 0)
                    {
                        upcomingChannels.Add(channel);
                    }
                }
            }

            // String to store the final text to print
            string textToPrint = string.Empty;

            // Check if there are channels currently live
            if (liveChannels.Count > 0)
            {
                // Build text for live channels
                string liveText = "Live ";
                foreach (YoutubeChannel channel in liveChannels)
                {
                    liveText += channel.Emoji;
                }
                liveText = liveText.Trim();
                liveText = liveText.Replace(' ', '-');
                textToPrint += liveText;
            }
            else
            {
                // Check if there are upcoming channels
                if (upcomingChannels.Count > 0)
                {
                    // Order upcoming channels by the time of the next stream
                    upcomingChannels = upcomingChannels.OrderBy(a => a.LatestLiveOrNext).ToList();

                    // Build text for upcoming channels
                    string upcomingText = "Next " + upcomingChannels[0].Emoji;

                    if (upcomingChannels.Count > 1)
                    {
                        upcomingText += upcomingChannels[1].Emoji;
                    }

                    upcomingText = upcomingText.Trim();
                    textToPrint += upcomingText;
                }
                else
                {
                    // No live or upcoming channels
                    textToPrint += "No streams";
                }
            }

            // Replace spaces with underscores in the final text
            return textToPrint.Replace(' ', '_');
        }


        /// <summary>
        /// Gets a list of channels that have scheduled streams and/or videos.
        /// </summary>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static string GetNextStreamsAndLives(YoutubeChannel[] channels)
        {
            // Get the current date and time
            DateTime now = DateTime.Now;

            // List to store channels currently live
            List<YoutubeChannel> liveChannels = new();

            // List to store upcoming channels
            List<YoutubeChannel> upcomingChannels = new();

            // Iterate through channels to categorize them
            foreach (YoutubeChannel channel in channels)
            {
                // Check if the channel is currently live
                if (channel.Live)
                {
                    liveChannels.Add(channel);
                }
            }

            // Iterate through channels to find upcoming channels
            foreach (YoutubeChannel channel in channels)
            {
                // Check if the channel is not in the live channels list
                if (!liveChannels.Contains(channel))
                {
                    // Check if the channel's latest live or next stream is in the future
                    if (DateTime.Compare(now, channel.LatestLiveOrNext) < 0)
                    {
                        upcomingChannels.Add(channel);
                    }
                }
            }

            // String to store the final text to print
            string textToPrint = string.Empty;

            // Check if there are channels currently live
            if (liveChannels.Count > 0)
            {
                // Build text for live channels
                string liveText = "Live now: ";
                foreach (YoutubeChannel channel in liveChannels)
                {
                    liveText += channel.Emoji + "-";
                }
                liveText = liveText.Trim('-');

                foreach (YoutubeChannel channel in liveChannels)
                {
                    liveText += "\n- " + Utils.CapitalizeFirstLetter(channel.Name) + " " + channel.Emoji + " | " + channel.LatestLiveOrNextUrl + " ";
                }

                textToPrint += liveText + "\n\n •❅─────────────── ooo ───────────────❅•\n\n";
            }

            // Check if there are upcoming channels
            if (upcomingChannels.Count > 0)
            {
                // Order upcoming channels by the time of the next stream
                upcomingChannels = upcomingChannels.OrderBy(a => a.LatestLiveOrNext).ToList();

                // Build text for upcoming channels
                string upcomingText = "Upcoming Streams: ";
                foreach (YoutubeChannel channel in upcomingChannels)
                {
                    upcomingText += "\n- " + Utils.CapitalizeFirstLetter(channel.Name) + " " + channel.Emoji + " | On <t:" + new DateTimeOffset(channel.LatestLiveOrNext.ToUniversalTime()).ToUnixTimeSeconds() + "> | ||<" + channel.LatestLiveOrNextUrl + ">||";
                }

                textToPrint += upcomingText + "\n";
            }
            else
            {
                textToPrint += "\nNo upcoming streams!\n";
            }

            textToPrint += "\n\n •❅─────────────── ooo ───────────────❅•\n\n";
            return textToPrint;
        }
    }
}
