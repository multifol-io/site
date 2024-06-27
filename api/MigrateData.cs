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
using System.Collections.Generic;
using System.Threading;

namespace api
{
    public static class MigrateData
    {
        private static Dictionary<string, string> DataStorage = new();

        [FunctionName("MigrateData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("MigrateData processed a request.");

            string copyCode = req.Query["copyCode"];
            string profileData = req.Query["profileData"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            copyCode = copyCode ?? data?.copyCode;
            profileData = profileData ?? data?.profileData;

            if (profileData != null)
            {
                var Http = new HttpClient();
                var domain = "multifol.io";

                var jsonEncoding = "{ \"profileData\":'"+profileData+"'}";
                var myHttpContent = new MyHttpContent(jsonEncoding);
                var securityKeyResponse = await Http.PostAsync($"https://api.{domain}/api/copydata", myHttpContent, CancellationToken.None);
                return new OkObjectResult(securityKeyResponse.Content);
            }
            else if (copyCode != null)
            {
                var Http = new HttpClient();
                var domain = "multifol.io";

                var jsonEncoding = "{ 'copyCode':'"+copyCode?.Trim()+"'}";
                var myHttpContent = new MyHttpContent(jsonEncoding);
                var familyDataResponse = await Http.PostAsync($"https://api.{domain}/api/copydata", myHttpContent, CancellationToken.None);
                return new OkObjectResult(familyDataResponse.Content);
            }

            return new BadRequestObjectResult(null);
        }
    }
}
