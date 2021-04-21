using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DelayedWeather : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<InstantWeather> _logger;

        public DelayedWeather(ILogger<InstantWeather> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var rng = new Random();
            var res = await Startup.client.GetAsync(Startup.cppServer + "/delayedweather");
            if (!res.IsSuccessStatusCode)
                return "An error occurred";
            return await res.Content.ReadAsStringAsync();
        }
    }
}
