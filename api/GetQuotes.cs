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
    public static class GetQuotes
    {
        [FunctionName("GetQuotes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetQuotes processed a request.");

            string ticker = req.Query["ticker"];
            string apikey = req.Query["apikey"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            ticker = ticker ?? data?.ticker;
            apikey = apikey ?? data?.apikey;

            string url = $"https://eodhd.com/api/real-time/{ticker}?fmt=json&api_token={apikey}";
            var httpClient = new HttpClient();
            try {
                var quoteDataJson = await httpClient.GetStringAsync(url);
                return new OkObjectResult(quoteDataJson);
            }
            catch (Exception ex) {
                return new BadRequestObjectResult(ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
