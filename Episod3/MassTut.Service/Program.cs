using Serilog.Events;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using MassTut.Components.Consumers;
using MassTransit.Courier.Contracts;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using MassTut.Contracts;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IO;

namespace MassTut.Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Debug()
            //    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            //    .Enrich.FromLogContext()
            //    .WriteTo.Console()
            //    .WriteTo.File("lot.txt")
            //    .CreateLogger();
            //Log.Logger.Information("Application Started!");

            var configBuilder = new ConfigurationBuilder();
            BuildConfig(configBuilder);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configBuilder.Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Application Starting");

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
                    services.AddMassTransit(cfg =>
                    {
                        services.AddMassTransit(cfg =>
                        {
                            //cfg.AddConsumer<SubmitOrderConsumer>();
                            
                            //cfg.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();
                            cfg.AddBus(ConfigureBus);
                            //cfg.AddBus(provicer => Bus.Factory.CreateUsingRabbitMq(config =>
                            //{
                            //    config.Host(new Uri(RabbitMqConsts.RabbitMqRootUri), h =>
                            //    {
                            //        h.Username(RabbitMqConsts.UserName);
                            //        h.Password(RabbitMqConsts.Password);
                            //    });
                            //}));
                        });
                    });

                    services.AddHostedService<MassTransitConsoleHostedService>();
                })
                .UseSerilog();
                //.ConfigureLogging((hostingContext, logging) =>
                //{
                //    logging.AddSerilog(dispose: true);
                //    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                //});

            

            if (isService)
                await builder.UseWindowsService().Build().RunAsync();
            else
                await builder.RunConsoleAsync();

            Log.CloseAndFlush();
        }

        private static IBusControl ConfigureBus(IBusRegistrationContext provicer)
        {
            return Bus.Factory.CreateUsingRabbitMq(config =>
            {
                config.Host(new Uri(RabbitMqConsts.RabbitMqRootUri), h =>
                {
                    h.Username(RabbitMqConsts.UserName);
                    h.Password(RabbitMqConsts.Password);
                });
                config.ReceiveEndpoint(RabbitMqConsts.NotificationServiceQueue, e =>
                {
                    e.Consumer<SubmitOrderConsumer>();
                });
            });
        }
        

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();
        }


    }
}
