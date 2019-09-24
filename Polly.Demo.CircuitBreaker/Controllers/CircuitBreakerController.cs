using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Polly.Demo.CircuitBreaker.Controllers
{
    [Route("api/circuitbreakerpolicy")]
    [ApiController]
    public class CircuitBreakerController : ControllerBase
    {

        private readonly HttpClient _httpClient;
        private readonly RetryPolicy<HttpResponseMessage> _httpRetryPolicy;

        private readonly CircuitBreakerPolicy<HttpResponseMessage> _breakerPolicy;

        public CircuitBreakerController(HttpClient httpClient, CircuitBreakerPolicy<HttpResponseMessage> breakerPolicy)
        {
            _breakerPolicy = breakerPolicy;
            _httpClient = httpClient;
            _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode).RetryAsync(3);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            string requestEndpoint = $"LegacyData/{id}";

            HttpResponseMessage response = await _httpRetryPolicy.ExecuteAsync(
                 () => _breakerPolicy.ExecuteAsync(
                     () => _httpClient.GetAsync(requestEndpoint)));

            if (response.IsSuccessStatusCode)
            {
                int itemsInStock = JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync());
                return Ok(itemsInStock);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }
    }
}
