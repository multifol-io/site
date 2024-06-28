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
using System.Reflection.Metadata;

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
                var copyToCode = await securityKeyResponse.Content.ReadAsStringAsync();
                log.LogInformation($"MigrateData returned {copyToCode}");
                return new OkObjectResult(copyToCode);
            }
            else if (copyCode != null)
            {
                var Http = new HttpClient();
                var domain = "multifol.io";

                var jsonEncoding = "{ 'copyCode':'"+copyCode?.Trim()+"'}";
                var myHttpContent = new MyHttpContent(jsonEncoding);
                var familyDataResponse = await Http.PostAsync($"https://api.{domain}/api/copydata", myHttpContent, CancellationToken.None);
                var familyData = await familyDataResponse.Content.ReadAsStringAsync();

                string content = familyData;
                int len = content.Length;
                if (len > 5) { content = content.Substring(5); }
            
                log.LogInformation($"MigrateData returned {len} chars starting with '{content}'");
                return new OkObjectResult(familyData);
            }

            log.LogInformation($"MigrateData returned BadRequestObjectResult");
            return new BadRequestObjectResult(null);
        }
    }
}
