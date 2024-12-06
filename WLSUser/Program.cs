using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace WLSUser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(100,100);
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            IWebHost host = CreateWebHostBuilder(args)
                .UseConfiguration(config)
                .Build();

            //Fully setup host at this point
            //Configure anything here

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}