using System;
using System.Threading;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Threading.Tasks;
using System.Net.Http;
using OpenTelemetry.Resources;

namespace CoreConsoleApp
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly ActivitySource activitySource = new ActivitySource("consoleapp");
        static async Task Main(string[] args)
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
              .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("consoleapp"))
              //.SetSampler(new AlwaysOnSampler())
              .AddSource("consoleapp")
              .AddHttpClientInstrumentation()
              //.AddConsoleExporter()
              .AddJaegerExporter(o =>
              {
                  o.AgentHost = JaegerOptions.Host;
                  o.AgentPort = JaegerOptions.Port;
              })
              .Build();

            

            while (true)
            {
                Console.WriteLine("Please enter number of seconds to wait before requesting temperature:");
                var line = Console.ReadLine();
                using (var activity = activitySource.StartActivity("WeatherRequest"))
                {
                    activity?.SetTag("input", line);
                    bool parse = int.TryParse(line, out int stime);
                    if (!parse || stime < 0)
                    {
                        activity?.SetTag("error", true);
                        continue;
                    }
                    await Wait(stime);
                    var res = await GetWeather();
                    Console.WriteLine("Weather response:");
                    Console.WriteLine(res);
                    if (stime > 0)
                        await Wait(1);
                }
            }
        }

        private static async Task Wait(int stime)
        {
            if (stime <= 0)
                return;
            using (var activity = Activity.Current?.Source.StartActivity("Wait", ActivityKind.Internal))
            {
                activity?.SetTag("sleepTime", $"{stime}s");
                await Task.Delay(stime * 1000);
            }
        }

        private static async Task<string> GetWeather()
        {
            var rng = new Random();
            string weathers;
            Activity.Current?.AddEvent(new ActivityEvent("Weather request starting"));
            if (rng.NextDouble() < 0.6)
                weathers = await client.GetStringAsync("http://localhost:5000/instantweather");
            else
                weathers = await client.GetStringAsync("http://localhost:5000/delayedweather");
            Activity.Current?.AddEvent(new ActivityEvent("Weather request finished"));
            return weathers;
        }
    }

    internal class JaegerOptions
    {
        public static string Host { get; set; } = "localhost";

        public static int Port { get; set; } = 6831;
    }
}
