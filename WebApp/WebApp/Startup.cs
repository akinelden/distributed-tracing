using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using System.Net.Http;

namespace WebApp
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public static readonly HttpClient client = new HttpClient();
        public static string cppServer;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            cppServer = configuration.GetValue<string>("CppServer");
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
            services.AddControllers();

            // Switch between Zipkin/Jaeger by setting UseExporter in appsettings.json.
            var exporter = this.Configuration.GetValue<string>("UseExporter").ToLowerInvariant();
            switch (exporter)
            {
                case "jaeger":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("Jaeger:ServiceName")))
                        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)))
                        //.AddProcessor(new BatchActivityExportProcessor())
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddJaegerExporter());

                    services.Configure<JaegerExporterOptions>(this.Configuration.GetSection("Jaeger"));
                    break;
                //case "zipkin":
                //    services.AddOpenTelemetryTracing((builder) => builder
                //        .AddAspNetCoreInstrumentation()
                //        .AddHttpClientInstrumentation()
                //        .AddZipkinExporter());

                //    services.Configure<ZipkinExporterOptions>(this.Configuration.GetSection("Zipkin"));
                //    break;
                //case "otlp":
                //    // Adding the OtlpExporter creates a GrpcChannel.
                //    // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
                //    // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
                //    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                //    services.AddOpenTelemetryTracing((builder) => builder
                //        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("Otlp:ServiceName")))
                //        .AddAspNetCoreInstrumentation()
                //        .AddHttpClientInstrumentation()
                //        .AddOtlpExporter(otlpOptions =>
                //        {
                //            otlpOptions.Endpoint = new Uri(this.Configuration.GetValue<string>("Otlp:Endpoint"));
                //        }));
                //    break;
                default:
                    services.AddOpenTelemetryTracing((builder) => builder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter());
                    break;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseCors("AllowAllOrigins");
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
