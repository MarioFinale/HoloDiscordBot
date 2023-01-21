using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace HoloDiscordBot
{
    public class YoutubeChannel
    {
        public string Name { get; }

        public string Emoji { get; }
        public string Url { get; }
        public string ExternalID { get; }
        public DateTime LatestLiveOrNext { get; }
        public string LatestLiveOrNextUrl { get; }
        public bool Live { get; }
        public YoutubeVideo[] Videos { get; }
        public YoutubeChannel(string name, string emoji)
        {
            Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "YoutubeChannel", "Loading " + name));
            Emoji = emoji;
            Name = name;
            ExternalID = getExternalIdOfYTChannelByUsername(name).Item1;
            Url = "https://www.youtube.com/@" + name;
            Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "YoutubeChannel", "Getting videos from " + ExternalID));
            XmlDocument doc = getfeedFromChannelByExternalID(ExternalID).Item1;
            string[] videos = getVideoUrlsFromXMLFeed(doc);
            string liveVideoUrl = string.Empty;
            Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "YoutubeChannel", "Sorting and processing videos from " + ExternalID));
            List<YoutubeVideo> videosList = new List<YoutubeVideo>();

            foreach (string video in videos)
            {
                YoutubeVideo YTvideo = GetVideoObjectFromUrl(video);
                if (YTvideo.Live)
                {
                    Live = true;
                    videosList.Add(YTvideo);
                    Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "YoutubeChannel", ExternalID + " is live! skipping processing of later videos..." + Name));
                    break;
                }
                videosList.Add(YTvideo);
            }
            videosList = videosList.OrderBy(a => a.Publish).Reverse().ToList();

            LatestLiveOrNextUrl = videosList.First().Url;
            LatestLiveOrNext = videosList.First().Publish;

            Videos = videosList.ToArray();
            Utils.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "YoutubeChannel", "Processed " + videosList.Count + " videos from " + Name));
        }



        YoutubeVideo GetVideoObjectFromUrl(string videoUrl)
        {
            Tuple<string, bool> response = Utils.GETWebResourceAsText(videoUrl);
            if (!response.Item2) return new YoutubeVideo(videoUrl, new DateTime(1990, 1, 1), false, videoUrl);//dummy year, we set it far ago so it goest to the end of the list when sorted by date
            Match broadcastMatch = Regex.Match(response.Item1, "\"liveBroadcastDetails\":.+?\"isLiveNow\":(.+?),.+?startTimestamp\":\"(.+?)\"");
            if (broadcastMatch.Success)
            {
                Match videoTitleMatch = Regex.Match(response.Item1, "meta name=\"title\" content=\"(.+?)\"");
                string videoTitle;
                if (videoTitleMatch.Success)
                {
                    videoTitle = videoTitleMatch.Groups[1].Value;
                }
                else
                {
                    videoTitle = videoUrl;
                }
                string[] dateVals = broadcastMatch.Groups[2].Value.Replace('T', '-').Replace(':', '-').Replace('+', '-').Split('-');
                int[] vals = Array.ConvertAll(dateVals, int.Parse);
                DateTime date = new DateTime(vals[0], vals[1], vals[2], vals[3], vals[4], vals[5]);
                bool live = broadcastMatch.Groups[1].Value.ToUpperInvariant().Trim().Equals("TRUE");
                return new YoutubeVideo(videoUrl, date, live, videoTitle);

            }
            else
            {
                Match videoTitleMatch = Regex.Match(response.Item1, "meta name=\"title\" content=\"(.+?)\"");
                string videoTitle;
                if (videoTitleMatch.Success)
                {
                    videoTitle = videoTitleMatch.Groups[1].Value;
                }
                else
                {
                    videoTitle = videoUrl;
                }

                Match videoUploadDatematch = Regex.Match(response.Item1, "itemprop=\"uploadDate\" content=\"(.+?)\"");
                if (videoUploadDatematch.Success)
                {
                    string[] dateVals = videoUploadDatematch.Groups[1].Value.Split('-');
                    DateTime date = new DateTime(int.Parse(dateVals[0]), int.Parse(dateVals[1]), int.Parse(dateVals[2]));
                    return new YoutubeVideo(videoUrl, date, false, videoTitle);
                }
                return new YoutubeVideo(videoUrl, new DateTime(1990, 1, 1), false, videoTitle);//dummy year again
            }
        }


        string[] getVideoUrlsFromXMLFeed(XmlDocument document)
        {
            if (document is null) return null;
            var xmlNodeList = document.ChildNodes;
            if (xmlNodeList is null) return null;
            XmlNodeList rootNodes = (XmlNodeList)xmlNodeList;
            XmlNode feed = rootNodes[1];
            XmlNodeList feedNodes = feed.ChildNodes;
            List<string> videos = new List<string>();

            foreach (XmlNode node in feedNodes)
            {
                if (node.Name.ToUpper(System.Globalization.CultureInfo.InvariantCulture).Contains("ENTRY"))
                {
                    string nodeContent = node.InnerXml.ToString();
                    Match m = Regex.Match(nodeContent, "media:content url=\"(.+?)\\?");
                    if (m.Success) videos.Add(m.Groups[1].Value);

                }
            }
            return videos.ToArray();
        }


        Tuple<XmlDocument, bool> getfeedFromChannelByExternalID(string externalID)
        {
            string feedUrl = "https://www.youtube.com/feeds/videos.xml?channel_id=" + externalID;
            Tuple<string, bool> response = Utils.GETWebResourceAsText(feedUrl);
            if (!response.Item2) return new Tuple<XmlDocument, bool>(new XmlDocument(), false);

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(response.Item1);
                return new Tuple<XmlDocument, bool>(doc, true);

            }
            catch (Exception)
            {
            }
            return new Tuple<XmlDocument, bool>(new XmlDocument(), false);
        }

        Tuple<string, bool> getExternalIdOfYTChannelByUsername(string userName)
        {
            string url = "https://www.youtube.com/@" + userName;
            Tuple<string, bool> response = Utils.GETWebResourceAsText(url);
            if (!response.Item2) return new Tuple<string, bool>(string.Empty, false);
            Match match = Regex.Match(response.Item1, "externalId\":\"(.+?)\"");
            if (match.Success)
            {
                return new Tuple<string, bool>(match.Groups[1].Value, true);
            }
            else
            {
                return new Tuple<string, bool>(string.Empty, false);
            }

        }


    }
}
