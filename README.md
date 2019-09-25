# PollyDemoApp
A small sample app demonstrating the Polly library.

.Net Core 2.2 Web API
Microsoft.Extensions.Http.Polly version 3.0
Polly.Demo.Server represents a legacy data API that simulates a 100 millisecond delay and only succeeds 1 out of 4 connections
Polly.Demo.Proxy represents a proxy API that uses the Retry Policy and the Wait and Retry Policy
Polly.Demo.CircuitBreaker represents a more advanced pattern

Most of these examples are modified versions of examples from Bryan Hogan's Pluralsight "Course Fault Tolerant Web Service Requests with Polly"
