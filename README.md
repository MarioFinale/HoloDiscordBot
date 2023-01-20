# HoloDiscordBot
Simple bot that updates a discord channel with ongoing and upcoming livestreams from a text list.

# How to use

 - Get a Discord API Key with GuildMessages and Guilds permissions (to allow cleaning a channel and set the channel name). Place that key in a text fille named "key.txt" on the same directory of the executable.
 - Add the bot to your server, give it permission to manage channels and send messages.
 - Add the channels ID to a text file named "channels.txt" on the same directory of the executable, one per line.
 - Create a text file named "list.txt" on the same directory of the executable with the format "Channel-Username|Emoji". Be careful to NOT add the "@", do not leave empty lines and do not leave spaces after or before each line.
 - Run the bot! If you followed the steps correctly it should just work!
