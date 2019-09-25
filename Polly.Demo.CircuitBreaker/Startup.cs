using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using Polly;
using Polly.CircuitBreaker;

namespace Polly.Demo.CircuitBreaker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            /* 
             The circuit breaker pattern allows you to monitor requests sent to a remote service
             and stop the request before they are sent if it's detected that the remote service is
             experiencing issues or has been returning errors

            It gets its name from the circuit breaker you would find in your home. 

            How does it work?

            -Monitors the percentage of requests failures over time, if there are more failures than the 
            specified threshold, the circuit breaks for a specified duration. 
            
            It also allows for a minimum throughput threshold - if more than 10% of requests fail 
            within a sixty second window with a minimu throughput of 100 requests, break the circuit for 10 seconds.
            This also means that if within 60 seconds 1 of 10 total requets fail, it will not break because it hasn't
            met that threshold.

            This also gives you the power to meet the SLA on non-premimium API connections. If an API only allows 100 requests
            per minute, you can use the circuit breaker pattern to throttle your requests to make sure you meet that SLA

            You can apply the policy to multiple endpoints from the same service, so that if one endpoint experiences errors, you could
            use the Fallback policy to present some default value. This is ideal of a monolithic webservice with multiple endpoint is failing. 
            Instead of continuing to attempt to connect with multiple requests, the client connection fails completely.

            Polly's circuit breaker pattern has three states

            ___./ .__   Open (Broken Circuit)
            __._.__     Closed (Request continue to flow)
            __...__     Half-open

            Closed to Open      - when a problem is detected
            Open to Half-open   - when the duration of the break is reached
            Half-open to Open   - if the first request fails
            Half-open to Closed - if the firs request is successful
             
            In the following we still have the same Handler/Behavior pattern as we did with the Retry policy
            The behavior is what is interesting:

            0.5 refers to the percentage of failed requests (50% failure rate)
            TimeSpan.FromSeconds(60) is the duration

            So if half the requests in a 60 second window fail, the circuit breaker is active

            7 is the minimum throughput in that 60 second window

            TimeSpan.FromSeconds(15) - is the time the circuit breaker will remain in an Open state (meaning, no data flows)

            OnBreak, OnReset, and OnHalfOpen are the delegates we'll use to handle the various states of the Circuit Breaker

             */
            CircuitBreakerPolicy<HttpResponseMessage> breakerPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .AdvancedCircuitBreakerAsync(0.5, TimeSpan.FromSeconds(60), 7, TimeSpan.FromSeconds(15),
        OnBreak, OnReset, OnHalfOpen);

            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:53974/api/") // this is the endpoint HttpClient will hit,
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            services.AddSingleton<HttpClient>(httpClient);
            services.AddSingleton<CircuitBreakerPolicy<HttpResponseMessage>>(breakerPolicy);
            services.AddMvc();


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        private void OnHalfOpen()
        {
            Debug.WriteLine("Connection half open");
        }

        private void OnReset(Context context)
        {
            Debug.WriteLine("Connection reset");
        }

        private void OnBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan timeSpan, Context context)
        {
            Debug.WriteLine($"Connection break: {delegateResult.Result}, {delegateResult.Result}");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
