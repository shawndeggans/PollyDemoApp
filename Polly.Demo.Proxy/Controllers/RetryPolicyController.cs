using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace Polly.Demo.Proxy.Controllers
{
    [Produces("application/json")]
    [Route("api/retrypolicy")]
    public class RetryPolicyController : Controller
    {
        //create our policy for retry 
        readonly RetryPolicy<HttpResponseMessage> _httpRetryPolicy;

        public RetryPolicyController()
        {
            //here I'm telling the retry policy that I want to retry at least 3 times if I encounter a failure
            //Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode).RetryAsync(3);
            _httpRetryPolicy = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .RetryAsync(3, (ex, retryCount) =>
                {
                    Console.WriteLine("Retry Count {0}", retryCount);
                });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var httpClient = GetHttpClient();
            string requestEndpoint = $"LegacyData/{id}";

            //HttpResponseMessage response = await httpClient.GetAsync(requestEndpoint);
            HttpResponseMessage response = await _httpRetryPolicy.ExecuteAsync(() => httpClient.GetAsync(requestEndpoint));

            if (response.IsSuccessStatusCode)
            {
                int itemsInStock = JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync());
                return Ok(itemsInStock);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }

        private HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(@"http://localhost:53974/api/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }


    }
}