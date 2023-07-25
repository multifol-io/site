using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace api {
    public static class ForumUtilities {

        public static async Task<string> GetTopicsFromForum(string forum, int start, string? search) {
            string url = $"https://www.bogleheads.org/forum/viewforum.php?f={forum}&start={start}";
            var httpClient = new HttpClient();
            string topics = null;
            var forumPageHtml = await httpClient.GetStringAsync(url);

            // <a href="./viewtopic.php?t=409019&amp;sid=0269177a9b5bb8119cf05d88ff03905c" class="topictitle">Considering shifting investment strategies to a Bogleheads approach</a>	
            int c = 0;
            var preTopic = "<a href=\"./viewtopic.php?t=";
            var preTopicLoc = forumPageHtml.IndexOf(preTopic, c);
            while (preTopicLoc > -1) {
                var startOfTopicId = preTopicLoc + preTopic.Length;
                var ampLoc = forumPageHtml.IndexOf("&", startOfTopicId);
                var topicStr = forumPageHtml[startOfTopicId..ampLoc];

                var contentStartLoc = forumPageHtml.IndexOf(">", ampLoc);
                var contentEndLoc = forumPageHtml.IndexOf("</a>", ampLoc);
                var titleStr = forumPageHtml[(contentStartLoc+1)..(contentEndLoc)];
                if (search == null || titleStr.ToLowerInvariant().Contains(search.ToLowerInvariant())) {
                    topics += topicStr + " " + titleStr + "\n";
                }

                c = ampLoc;
                preTopicLoc = forumPageHtml.IndexOf(preTopic, c);
            }
            
            return topics;
        }

        public static async Task<string> GetTopicPost(string topic, string start, string? cachePath) {
            string topicPageHtml = null;
            string htmlFileName = null;

            if (cachePath != null) {
                htmlFileName = Path.Combine(cachePath, topic + ".html");
                if (File.Exists(htmlFileName)) {
                    topicPageHtml = await File.ReadAllTextAsync(htmlFileName);
                }
            }

            if (topicPageHtml == null) {
                string url = $"https://www.bogleheads.org/forum/viewtopic.php?t={topic}&start={start}";
                var httpClient = new HttpClient();
                topicPageHtml = await httpClient.GetStringAsync(url);

                if (cachePath != null) {
                    await File.WriteAllTextAsync(htmlFileName, topicPageHtml);
                }
            }
            
            int c = 0;
            var preContent = "<div class=\"content\">";
            var preContentLoc = topicPageHtml.IndexOf(preContent, c);
            var startOfContent = preContentLoc + preContent.Length;
            var endOfContent = topicPageHtml.IndexOf("</div>", startOfContent);
            var content = topicPageHtml[startOfContent..endOfContent];
            return content;
        }
    }
}