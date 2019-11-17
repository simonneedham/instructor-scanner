using InstructorScanner.Core;
using InstructorScanner.FunctionApp;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using TradeTeq.AzureFunctions;

[assembly: FunctionsStartup(typeof(Startup))]
namespace TradeTeq.AzureFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();

            builder.Services.Configure<AppSettings>(config.GetSection("AppSettings"));

            ConfigureServices(builder);
        }

        private void ConfigureServices(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<IStorageHelper, StorageHelper>();
        }
    }
}