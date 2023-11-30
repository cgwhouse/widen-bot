using System;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WidenBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var builder = new HostApplicationBuilder(args);

            builder
                .Services
                .AddLavalink()
                .ConfigureLavalink(config =>
                {
                    config.ReadyTimeout = TimeSpan.FromSeconds(10);
                    config.Label = "WidenBot";
                    config.Passphrase = "adminadmin_2";
                });

            builder.Build().Run();
        }
    }
}
