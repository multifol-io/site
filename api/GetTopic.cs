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
    public static class GetTopic
    {
        [FunctionName("GetTopic")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetForum processed a request.");

            string topic = req.Query["topic"];
            string start = req.Query["start"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            topic = topic ?? data?.topic;
            start = start ?? data?.start;

            try {
                var content = await ForumUtilities.GetTopicPost(topic, start);
                return new OkObjectResult(content);
            }
            catch (Exception ex) {
                return new BadRequestObjectResult(ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
