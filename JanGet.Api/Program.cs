using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Log73.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JanGet.Api
{
    public class Program
    {
        public static Config Config { get; private set; }

        public static async Task Main(string[] args)
        {
            if (!Directory.Exists("./files"))
                Directory.CreateDirectory("./files");
            Log73.Console.Options.UseAnsi = false;
            Config = await JsonSerializer.DeserializeAsync<Config>(new FileStream("./config.json", FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite));
            Mongo.Connect();
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                // .ConfigureLogging(builder =>
                // {
                //     builder.ClearProviders();
                //     builder.AddLog73Logger();
                // })
            ;
    }
}