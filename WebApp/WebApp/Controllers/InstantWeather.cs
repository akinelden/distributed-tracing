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
    public class InstantWeather : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<InstantWeather> _logger;

        public InstantWeather(ILogger<InstantWeather> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var rng = new Random();
            var res = await Startup.client.GetAsync(Startup.cppServer + "/instantweather");
            if (!res.IsSuccessStatusCode)
                return "An error occurred: Akın işten döndü mü?";
            return await res.Content.ReadAsStringAsync();
        }
    }
}
