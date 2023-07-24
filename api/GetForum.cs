using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace api
{
    public static class GetForum
    {
        [FunctionName("GetForum")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetForum processed a request.");

            string forum = req.Query["forum"];
            string start = req.Query["start"];
            string search = req.Query["search"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            forum = forum ?? data?.forum;
            start = start ?? data?.start;
            search = search ?? data?.search;

            string url = $"https://www.bogleheads.org/forum/viewforum.php?f={forum}&start={start}";
            var httpClient = new HttpClient();
            string topics = null;
            try {
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
                
                return new OkObjectResult(topics);
            }
            catch (Exception ex) {
                return new BadRequestObjectResult(ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
