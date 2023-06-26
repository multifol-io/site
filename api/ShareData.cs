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
    public static class ShareData
    {
        private static Dictionary<string, string> DataStorage = new();

        [FunctionName("ShareData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SearchTicker processed a request.");

            string importCode = req.Query["importCode"];
            string profileData = req.Query["profileData"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            importCode = importCode ?? data?.importCode;
            profileData = profileData ?? data?.profileData;

            if (profileData != null)
            {
                bool foundUniqueSecurityString = false;
                string securityString = null;
                int maxCount = 10;
                while (!foundUniqueSecurityString && maxCount > 0)
                {
                    maxCount--;
                    int security = (int)(Random.Shared.NextDouble() * 999999.0);
                    securityString = security.ToString();

                    if (!DataStorage.ContainsKey(securityString)) {
                        foundUniqueSecurityString = true;
                        DataStorage.Add(securityString, profileData);
                        return new OkObjectResult(securityString);
                    }
                }

                return new BadRequestObjectResult("no security string possible");
            }
            else if (importCode != null)
            {
                if (DataStorage.ContainsKey(importCode))
                {
                    var result = DataStorage[importCode];
                    DataStorage.Remove(importCode);
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
