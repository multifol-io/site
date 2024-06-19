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

namespace api
{
    public static class CopyData
    {
        private static Dictionary<string, string> DataStorage = new();

        [FunctionName("CopyData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CopyData processed a request.");

            string copyCode = req.Query["copyCode"];
            string profileData = req.Query["profileData"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            copyCode = copyCode ?? data?.copyCode;
            profileData = profileData ?? data?.profileData;

            if (profileData != null)
            {
                bool foundUniqueCopyCode = false;
                string newCopyCode = null;
                int maxCount = 10;
                while (!foundUniqueCopyCode && maxCount > 0)
                {
                    maxCount--;
                    long security = (long)(Random.Shared.NextDouble() * 999999999.0);
                    newCopyCode = security.ToString();

                    if (!DataStorage.ContainsKey(newCopyCode)) {
                        foundUniqueCopyCode = true;
                        DataStorage.Add(newCopyCode, profileData);
                        return new OkObjectResult(newCopyCode);
                    }
                }

                return new BadRequestObjectResult("no security string possible");
            }
            else if (copyCode != null)
            {
                if (DataStorage.ContainsKey(copyCode))
                {
                    var result = DataStorage[copyCode];
                    DataStorage.Remove(copyCode);
                    return new OkObjectResult(result);
                }
                else
                {
                    return new BadRequestObjectResult(null);
                }
            }

            return new BadRequestObjectResult(null);
        }
    }
}
