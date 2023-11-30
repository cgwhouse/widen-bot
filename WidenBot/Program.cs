using System;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WidenBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new HostApplicationBuilder(args);

            builder
                .Services
                .AddLavalink()
                .ConfigureLavalink(config =>
                {
                    config.ReadyTimeout = TimeSpan.FromSeconds(10);
                    config.Label = "WidenBot";
                    config.Passphrase = "adminadmin_2";
                })
                .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));

            builder.Build().Run();
        }
    }
}
