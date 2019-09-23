using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Polly.Demo.Controllers
{
    [Produces("application/json")]
    [Route("api/LegacyData")]
    [ApiController]
    public class LegacyDataController : ControllerBase
    {

            static int _requestCount = 0;

            [HttpGet("{id}")]
            public async Task<IActionResult> Get(int id)
            {

                await Task.Delay(100); //simulate a 100 millisecond delay while some data processes on the backend system
                _requestCount++;

                if (_requestCount % 4 == 0) // only 1 out of 4 requests will succeed - really crappy API
                {
                    return Ok(15);
                }


                return StatusCode((int)HttpStatusCode.InternalServerError, "Something went wrong.");
            }

        }
    }
