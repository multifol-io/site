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

            try {
                var topics = await ForumUtilities.GetTopicsFromForum(forum, int.Parse(start), search);
                return new OkObjectResult(topics);
            }
            catch (Exception ex) {
                return new BadRequestObjectResult(ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
