namespace SampleWebApp;

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VaultSharp.Extensions.Configuration;

public class Program
{
    public static ILogger Logger { get; private set; }

    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                using var loggerFactory = LoggerFactory.Create(builder =>
                    builder.AddConsole(c => c.LogToStandardErrorThreshold = LogLevel.Debug).AddDebug());
                Logger = loggerFactory.CreateLogger<Program>();

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile(
                        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json",
                        optional: true)
                    .AddEnvironmentVariables()
                    .AddVaultConfiguration(
                        () => new VaultOptions(
                            "http://vault:8200", // Change this to "http://localhost:8200" if you are working locally without Docker
                            "root",
                            reloadOnChange: true,
                            reloadCheckIntervalSeconds: 60),
                        "sampleapp",
                        "secret",
                        Logger);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
